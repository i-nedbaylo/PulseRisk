# Глава 25. Добавляем quote-driven risk evaluation

## Цель главы

Связать поток котировок с Risk Engine.

До этой главы риск пересчитывался после изменения позиции: сделка обновляла `Position`, публиковался `PositionChangedEvent`, затем worker создавал `RiskEvaluationRequested`.

Но в real-time risk-management этого мало. Даже если позиция не менялась, новая цена может изменить floating PnL, equity, margin level и привести к alert-у. Значит, котировки тоже должны запускать risk evaluation.

## Демонстрируемые компетенции

Глава показывает:

- понимание producer-consumer pipeline;
- аккуратную работу с bounded channels;
- защиту от ошибочного multi-consumer дизайна;
- quote-driven перерасчет risk metrics;
- sampling внутри batch-а;
- application-port для поиска открытых позиций;
- unit-тестируемый dispatcher вместо тяжелого тестирования `BackgroundService`;
- применение GoF/GRASP по делу.

## Что уже есть

К началу главы есть:

- `MarketDataSimulatorWorker`, который пишет `QuoteTick`;
- quote channel с режимом `DropOldest`;
- `QuoteBatchWriterWorker`;
- `IQuoteBatchWriter`;
- `EfQuoteBatchWriter`, который сохраняет котировки в PostgreSQL;
- `RiskEvaluationRequested` channel;
- `RiskEvaluationRequestedWorker`;
- `RiskEvaluationProcessor`;
- `RiskAlertFactory` и active-alert deduplication.

Теперь нужно соединить quote pipeline и risk pipeline.

## Важное архитектурное ограничение

`Channel<T>` в текущей реализации - это work queue, а не pub/sub.

Это значит: если подключить к одному `IEventReader<QuoteTick>` два worker-а, они не получат одинаковые сообщения. Они начнут конкурировать за ticks.

Плохая схема:

```text
QuoteChannel -> QuoteBatchWriterWorker
QuoteChannel -> QuoteRiskWorker
```

В такой схеме часть котировок уйдет batch writer-у, а часть risk worker-у. Это не broadcast.

Поэтому в этой главе выбираем другой путь:

```text
QuoteChannel
  -> QuoteBatchWriterWorker
      -> save quotes
      -> QuoteRiskEvaluationDispatcher
          -> RiskEvaluationRequested channel
```

Risk evaluation запускается после успешной записи batch-а котировок.

## Что сделаем сейчас

Добавим:

- метод `IPositionRepository.ListOpenBySymbolAsync(...)`;
- EF-реализацию поиска открытых позиций по символу;
- `QuoteRiskEvaluationDispatcher`;
- вызов dispatcher-а после `IQuoteBatchWriter.WriteAsync(...)`;
- DI-регистрацию dispatcher-а;
- unit-тесты dispatcher-а.

## Паттерны GoF/GRASP

### Producer-Consumer

`MarketDataSimulatorWorker` производит `QuoteTick`, `QuoteBatchWriterWorker` потребляет их с собственной скоростью.

После сохранения batch-а dispatcher производит `RiskEvaluationRequested`, а `RiskEvaluationRequestedWorker` потребляет эти события.

Так система состоит из двух связанных producer-consumer участков:

- quote ingestion;
- risk evaluation.

### GRASP Controller / Pure Fabrication

`QuoteRiskEvaluationDispatcher` не является доменной сущностью. Это orchestration-компонент.

Его задача:

- принять сохраненный batch котировок;
- выбрать последнюю котировку по каждому символу;
- найти открытые позиции;
- опубликовать risk requests.

Это Pure Fabrication: искусственный класс, который нужен для связности и тестируемости.

### GRASP Indirection

Dispatcher зависит от:

- `IPositionRepository`;
- `IEventWriter<RiskEvaluationRequested>`.

Он не знает про EF Core, `PulseRiskDbContext` или конкретную реализацию channel.

### Protected Variations

Если позже появится Kafka, RabbitMQ, Redis Stream или настоящий pub/sub, dispatcher можно заменить или перенести, не переписывая Risk Engine.

Если появится `LatestQuoteCache`, изменится источник latest quote, но контракт risk request останется прежним.

### Почему не Observer

На первый взгляд котировки можно представить как subject, а Risk Engine как observer.

Но текущая реализация намеренно использует explicit channels. Это проще контролировать:

- есть backpressure;
- видна глубина очереди;
- можно выбрать full mode;
- проще тестировать producer и consumer отдельно.

Поэтому Observer как GoF-паттерн здесь не нужен.

## Реализация шаг за шагом

### Шаг 1. Расширяем IPositionRepository

Risk dispatcher должен знать, какие позиции затронуты новой котировкой.

Добавляем метод:

```csharp
Task<IReadOnlyCollection<Position>> ListOpenBySymbolAsync(
    Symbol symbol,
    CancellationToken cancellationToken);
```

Метод возвращает только открытые позиции:

```text
position.NetVolume != 0
```

Flat position не нуждается в quote-driven risk evaluation.

### Шаг 2. Реализуем EF-запрос

В `EfPositionRepository` используется read-only query:

```csharp
dbContext.Positions
    .AsNoTracking()
    .Where(position => position.Symbol == symbol && position.NetVolume != 0)
```

`AsNoTracking` важен, потому что dispatcher не изменяет позиции. Он только читает их для публикации risk requests.

### Шаг 3. Создаем QuoteRiskEvaluationDispatcher

Dispatcher получает batch `QuoteTick`.

Внутри batch-а может быть несколько котировок одного символа. Для risk evaluation нам достаточно последней:

```csharp
var latestTicks = ticks
    .Select(tick => new QuoteRiskTick(new Symbol(tick.Symbol), tick))
    .GroupBy(item => item.Symbol)
    .Select(group => group.OrderByDescending(item => item.Tick.Timestamp).First())
    .ToArray();
```

Это маленький sampling-механизм.

Если за flush interval пришло 100 котировок `EURUSD`, мы не публикуем 100 risk requests на одну и ту же позицию. Мы публикуем один request от последней цены.

### Шаг 4. Публикуем RiskEvaluationRequested

Для каждой открытой позиции dispatcher пишет событие:

```csharp
new RiskEvaluationRequested(
    position.ClientId,
    position.TradingAccountId,
    latestTick.Symbol.Value,
    TradeId: null,
    PositionId: position.Id,
    Reason: "QuoteTick",
    RequestedAt: latestTick.Tick.Timestamp)
```

`TradeId` отсутствует, потому что источник события - не сделка.

`PositionId` есть, потому что котировка влияет на конкретную открытую позицию.

`Reason = "QuoteTick"` позволяет в логах отличить quote-driven evaluation от position-driven evaluation.

### Шаг 5. Подключаем dispatcher после batch insert

В `QuoteBatchWriterWorker` после:

```csharp
await writer.WriteAsync(batch, cancellationToken);
```

вызывается:

```csharp
await riskDispatcher.PublishAsync(batch, cancellationToken);
```

Порядок важен.

Если сначала опубликовать risk request, `RiskEvaluationContextBuilder` может прочитать из PostgreSQL предыдущую котировку. После сохранения batch-а `EfLatestQuoteReader` уже видит свежие данные.

### Шаг 6. Обновляем DI

`QuoteRiskEvaluationDispatcher` регистрируется как scoped dependency.

Это соответствует тому, что он использует `IPositionRepository`, а EF repositories живут в scoped lifetime.

`QuoteBatchWriterWorker` остается singleton hosted service, но на каждый flush создает scope.

## Почему не добавляем второй reader quote channel

Это важная ошибка, которую легко допустить.

Если сделать:

```text
QuoteBatchWriterWorker читает QuoteTick
QuoteRiskWorker читает QuoteTick
```

оба worker-а будут читать из одного channel как competing consumers.

В результате:

- batch writer может не сохранить часть ticks;
- risk worker может не увидеть часть ticks;
- поведение будет зависеть от scheduling;
- тесты станут случайными.

Для broadcast нужен другой механизм: отдельные channels на каждого consumer-а, explicit dispatcher, message broker или fan-out abstraction.

В текущем проекте проще и честнее сделать dispatch после batch write.

## Почему latest quote читается из PostgreSQL

В плане проекта есть future пункт `LatestQuoteCache`.

Но в этой итерации мы его не добавляем. Risk Engine уже умеет читать latest quote через `ILatestQuoteReader`, а infrastructure adapter берет последнюю котировку из PostgreSQL.

Это чуть медленнее cache, зато:

- меньше moving parts;
- проще consistency;
- проще объяснить порядок событий;
- уже покрыто текущими abstraction boundaries.

Cache можно добавить позже как optimization, когда появятся измерения.

## Проверка результата

Добавлены unit-тесты `QuoteRiskEvaluationDispatcherTests`.

Они проверяют:

- dispatcher публикует risk request по последней котировке символа;
- flat position игнорируется;
- несколько открытых позиций одного символа получают отдельные requests;
- при отсутствии открытых позиций события не публикуются.

Команда:

```bash
dotnet test PulseRisk.slnx
```

Результат текущей итерации:

- unit-тесты: `62/62`;
- integration tests: `6/8`;
- skipped integration tests: `2/8`.

Оба skipped tests зависят от Docker/Testcontainers PostgreSQL.

## Что изменилось в репозитории

- `IPositionRepository` получил `ListOpenBySymbolAsync(...)`.
- `EfPositionRepository` реализует read-only поиск открытых позиций по символу.
- Добавлен `QuoteRiskEvaluationDispatcher`.
- `QuoteBatchWriterWorker` после успешного flush публикует quote-driven risk requests.
- DI регистрирует dispatcher.
- Добавлены unit-тесты dispatcher-а.
- Обновлены `DEVELOPMENT_PLAN.md`, `ARCHITECTURE.md`, `README.md`, `book/progress.md`.

## Альтернативы

- **Второй consumer quote channel**: неверно для текущего `Channel<T>`, потому что это competing consumers.
- **Pub/sub abstraction для QuoteTick**: архитектурно чище для многих consumers, но преждевременно для текущего учебного монолита.
- **LatestQuoteCache прямо сейчас**: быстрее для hot path, но добавляет invalidation/consistency вопросы без измерений.
- **Risk request на каждый tick**: ближе к real-time, но может создать лавину событий при high frequency.
- **Risk request только по timer sampling**: стабильнее под нагрузкой, но хуже реактивность. Можно добавить позже как throttling policy.

## Частые ошибки

- Считать `Channel<T>` broadcast-механизмом.
- Публиковать risk request до сохранения котировки.
- Запускать risk evaluation для flat positions.
- Делать один risk request на каждый tick внутри batch-а.
- Загружать tracked EF entities для read-only dispatch.
- Смешивать batch persistence и risk calculation в одном классе.

## Вопросы для интервью

- Почему второй reader у quote channel был бы ошибкой?
- Почему risk request публикуется после batch insert?
- Почему dispatcher выбирает последнюю котировку по символу?
- Чем quote-driven risk evaluation отличается от position-driven?
- Когда понадобится `LatestQuoteCache`?
- Как бы вы изменили архитектуру при переходе на Kafka?
- Где лучше реализовать throttling quote-driven risk requests?

## Чек-лист главы

- [x] `IPositionRepository.ListOpenBySymbolAsync(...)` добавлен.
- [x] `EfPositionRepository` ищет открытые позиции по символу.
- [x] `QuoteRiskEvaluationDispatcher` добавлен.
- [x] Dispatcher выбирает последний tick по символу.
- [x] Dispatcher публикует `RiskEvaluationRequested`.
- [x] `QuoteBatchWriterWorker` вызывает dispatcher после batch insert.
- [x] DI обновлен.
- [x] Unit-тесты dispatcher-а добавлены.
- [x] Код собирается.
- [x] Проверки выполнены.
