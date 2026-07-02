# Глава 29. Добавляем нагрузочный сценарий

## Цель главы

Добавить первый воспроизводимый load-test сценарий для PulseRisk.

В предыдущей главе мы доказали, что отдельные PostgreSQL hot-path запросы используют ожидаемые индексы. Но `EXPLAIN ANALYZE` отвечает только на вопрос "как выполняется конкретный SQL-запрос". Он не показывает поведение всего backend pipeline:

- как быстро создаются сделки;
- успевают ли bounded channels принимать события;
- появляются ли dropped quotes;
- успевает ли Risk Engine читать risk requests;
- какая latency у синхронного write path `CreateTradeHandler`;
- сколько active alerts остается в persistence.

Поэтому в этой главе появляется первый load scenario.

Он пока не заменяет полноценный k6/JMeter/Gatling-тест. Его задача скромнее и практичнее: дать локальный, включаемый конфигурацией сценарий, который проходит через реальные application handlers, in-process channels, PostgreSQL и background workers.

## Демонстрируемые компетенции

Глава показывает:

- проектирование нагрузки без прямой вставки данных в БД;
- `BackgroundService` как запускаемый сценарий;
- options validation для опасной функциональности;
- генерацию клиентов, счетов, сделок и котировок;
- сбор throughput и latency;
- использование `IEventChannelMonitor` для диагностики backpressure;
- markdown-отчет в `docs/load-tests`;
- применение GoF/GRASP без лишнего framework-а.

## Требования к сценарию

По плану глава должна закрыть:

- `LoadTestWorker`;
- генерацию клиентов;
- генерацию счетов;
- генерацию сделок;
- генерацию котировок 500/sec;
- профиль 1000/sec;
- профиль 5000/sec как optional senior scenario;
- метрики:
  - trades/sec;
  - quotes/sec;
  - risk evaluations/sec;
  - average latency;
  - p95 latency;
  - dropped quotes;
  - active alerts;
- отчет в `docs/load-tests`;
- README-раздел с результатами.

## Архитектурное решение

Сценарий встроен как отключаемый hosted worker:

```text
LoadTestWorker
  -> LoadTestScenarioRunner
      -> CreateClientHandler
      -> CreateTradingAccountHandler
      -> CreateTradeHandler
      -> IEventWriter<QuoteTick>
      -> IEventChannelMonitor
      -> IRiskAlertRepository.CountActiveAsync
      -> ILoadTestReportWriter
```

Почему не отдельный console project?

Отдельный console runner тоже возможен. Но текущий проект уже имеет hosted services, DI, options и background pipeline. Для первой итерации удобнее включать сценарий тем же способом, что и остальные фоновые процессы.

Почему не HTTP?

HTTP-level load test будет нужен позже. Но сейчас цель - проверить backend pipeline: application handlers, PostgreSQL, channels, batch writer и Risk Engine. HTTP добавил бы сериализацию, routing и сетевой шум. Это полезно, но не для первого внутреннего baseline.

Почему не прямой insert в PostgreSQL?

Прямая вставка дала бы красивые цифры, но обошла бы:

- validators;
- transaction boundary trade + position;
- optimistic concurrency retry;
- публикацию `PositionChangedEvent`;
- Risk Engine flow.

Поэтому сделки создаются через `CreateTradeHandler`, а котировки идут через `IEventWriter<QuoteTick>`.

## Паттерны GoF/GRASP

### Template Method

`LoadTestWorker` наследует `BackgroundService`.

Framework задает lifecycle:

```text
StartAsync -> ExecuteAsync -> StopAsync
```

Проект реализует только сценарий внутри `ExecuteAsync`.

Это не самодельный Template Method, а аккуратное использование framework lifecycle.

### GRASP Controller / Pure Fabrication

`LoadTestScenarioRunner` - не доменная сущность.

Он существует потому, что кому-то нужно координировать сценарий:

- seed clients/accounts;
- запуск генерации quotes/trades;
- ожидание drain window;
- сбор snapshots;
- расчет отчета.

Если бы эта логика осталась в `LoadTestWorker`, hosted service быстро превратился бы в большой класс с несколькими причинами изменения.

### GRASP Indirection

Runner не знает о конкретном `PulseRiskDbContext` при создании сделок. Он работает через application handlers.

Для котировок он использует `IEventWriter<QuoteTick>`, а не вызывает `QuoteBatchWriterWorker` напрямую.

Это сохраняет production-like путь:

```text
QuoteTick -> quote channel -> QuoteBatchWriterWorker -> PostgreSQL -> QuoteRiskEvaluationDispatcher
```

### Protected Variations

Формат отчета вынесен за интерфейс:

```csharp
public interface ILoadTestReportWriter
{
    Task WriteAsync(LoadTestReport report, string path, CancellationToken cancellationToken);
}
```

Сейчас реализация пишет markdown. Позже можно добавить JSON, HTML или публикацию artifact-а в CI без изменения генератора нагрузки.

### Почему не Strategy

Профили `Quotes500`, `Quotes1000`, `Quotes5000` пока отличаются только настройками частоты.

Для них не нужен отдельный Strategy-класс. Достаточно enum-а и options mapping. Strategy понадобится, если появятся разные алгоритмы нагрузки:

- price spike;
- burst traffic;
- mixed read/write;
- concurrent same-position trades;
- long-running soak test.

## Реализация шаг за шагом

### Шаг 1. Добавляем LoadTestOptions

Появился options-класс:

```csharp
public sealed class LoadTestOptions
{
    public bool Enabled { get; init; }
    public bool StopApplicationWhenCompleted { get; init; }
    public LoadTestProfile Profile { get; init; } = LoadTestProfile.Quotes500;
    public int ClientCount { get; init; } = 10;
    public int AccountsPerClient { get; init; } = 1;
    public int DurationSeconds { get; init; } = 30;
    public int DrainSeconds { get; init; } = 5;
    public int QuotesPerSecond { get; init; }
    public int TradesPerSecond { get; init; }
}
```

`Enabled=false` по умолчанию.

Это принципиально: нагрузочный worker не должен случайно стартовать при обычном запуске API.

### Шаг 2. Добавляем профили

Профили:

```csharp
public enum LoadTestProfile
{
    Quotes500,
    Quotes1000,
    Quotes5000
}
```

Если `QuotesPerSecond` и `TradesPerSecond` не заданы явно, профиль подставляет defaults:

| Profile | Quotes/sec | Trades/sec |
| --- | ---: | ---: |
| `Quotes500` | 500 | 25 |
| `Quotes1000` | 1000 | 50 |
| `Quotes5000` | 5000 | 100 |

Для локального отчета мы переопределили trades/sec до `10`, чтобы короткий run не создавал слишком много строк.

### Шаг 3. Валидируем настройки

`LoadTestOptionsValidator` проверяет настройки только когда `Enabled=true`.

Это позволяет держать секцию в `appsettings.json`, но не требовать валидного load-test окружения для обычной разработки.

Проверяются:

- непустой список символов;
- client/account counts;
- duration/drain;
- rate limits;
- balance/leverage;
- report path.

### Шаг 4. Создаем LoadTestWorker

`LoadTestWorker` делает минимум:

1. Читает options.
2. Если сценарий выключен - логирует и завершает работу.
3. Создает scope.
4. Запускает `LoadTestScenarioRunner`.
5. Пишет отчет через `ILoadTestReportWriter`.
6. Если `StopApplicationWhenCompleted=true`, останавливает приложение.

Последний пункт удобен для CI и локального one-shot запуска:

```text
start app -> run scenario -> write report -> stop app
```

### Шаг 5. Создаем клиентов и счета

Runner создает данные через application handlers:

```text
CreateClientHandler
CreateTradingAccountHandler
```

Каждый вызов идет через отдельный DI scope. Это похоже на обычные HTTP requests и не копит tracked EF entities в одном долгоживущем `DbContext`.

### Шаг 6. Генерируем котировки

Котировки пишутся в quote channel:

```csharp
await quoteTicks.WriteAsync(new QuoteTick(symbol, bid, ask, timestamp), token);
```

Дальше их обрабатывает уже существующий pipeline:

```text
QuoteTick
-> InMemoryEventChannel<QuoteTick>
-> QuoteBatchWriterWorker
-> EfQuoteBatchWriter
-> PostgreSQL quotes
-> QuoteRiskEvaluationDispatcher
-> RiskEvaluationRequested
```

Так сценарий проверяет не только генератор, но и batch writer/backpressure boundary.

### Шаг 7. Генерируем сделки

Сделки создаются через:

```text
CreateTradeHandler.HandleAsync(...)
```

На каждую сделку измеряется latency:

```text
Stopwatch.StartNew()
-> CreateTradeHandler
-> SaveChanges
-> PositionChangedEvent
-> elapsed
```

Это latency синхронного write path, а не полного risk evaluation. Risk evaluation происходит асинхронно и отражается через channel metrics.

### Шаг 8. Ждем drain window

После окончания генерации runner не пишет отчет сразу.

Он ждет drain window, чтобы background workers успели:

- дочитать quote channel;
- сбросить batch котировок;
- опубликовать quote-driven risk requests;
- обработать `PositionChangedEvent`;
- обработать `RiskEvaluationRequested`.

Важная деталь: `QuoteBatchWriterWorker` держит текущий batch внутри себя. Channel depth может быть `0`, но batch еще не сброшен. Поэтому runner ждет минимум один `QuoteBatchOptions.FlushIntervalMilliseconds + 250ms`.

Это исправляет ошибку раннего отчета, когда последние котировки попадали в PostgreSQL уже после записи markdown.

### Шаг 9. Считаем метрики

Runner берет snapshots у всех `IEventChannelMonitor` до и после сценария.

Отчет содержит:

- quotes generated;
- trades attempted/succeeded/failed;
- actual quotes/sec;
- actual trades/sec;
- risk evaluations/sec;
- dropped quotes;
- active alerts;
- average trade latency;
- p95 trade latency;
- deltas по каждому channel.

`Active alerts` берутся из PostgreSQL через:

```csharp
IRiskAlertRepository.CountActiveAsync(...)
```

Это лучше, чем считать только события `RiskAlertRaisedEvent`: event показывает публикацию, а repository показывает фактическое состояние persistence.

### Шаг 10. Пишем markdown report

`MarkdownLoadTestReportWriter` сохраняет отчет.

Если путь относительный, writer ищет корень репозитория по `PulseRisk.slnx`. Это нужно потому, что `dotnet run --project src/PulseRisk.Api` может запускать процесс с working directory внутри API-проекта, а документация должна попадать в корневой `docs/load-tests`.

## Локальный прогон

Для первого отчета использовался disposable PostgreSQL 18 cluster.

Миграции применялись командой:

```bash
dotnet ef database update \
  --project src/PulseRisk.Infrastructure \
  --startup-project src/PulseRisk.Api \
  --connection "$PULSERISK_CONNECTION"
```

API запускался с включенным one-shot load test:

```bash
LoadTest__Enabled=true
LoadTest__StopApplicationWhenCompleted=true
LoadTest__Profile=Quotes500
LoadTest__ClientCount=5
LoadTest__AccountsPerClient=1
LoadTest__DurationSeconds=3
LoadTest__DrainSeconds=5
LoadTest__QuotesPerSecond=500
LoadTest__TradesPerSecond=10
LoadTest__ReportPath=docs/load-tests/2026-07-02-local-load-report.md
```

Результат сохранен в:

```text
docs/load-tests/2026-07-02-local-load-report.md
```

Ключевые цифры:

| Metric | Value |
| --- | ---: |
| Quotes generated | 1463 |
| Trades attempted | 28 |
| Trades succeeded | 28 |
| Trades failed | 0 |
| Actual quotes/sec | 486.94 |
| Actual trades/sec | 9.32 |
| Risk evaluations/sec | 12.54 |
| Dropped quotes | 0 |
| Active alerts | 0 |
| Average trade latency | 11.04 ms |
| p95 trade latency | 16.55 ms |

Channel deltas:

| Channel | Written | Read | Dropped |
| --- | ---: | ---: | ---: |
| `QuoteTick` | 1463 | 1463 | 0 |
| `PositionChangedEvent` | 28 | 28 | 0 |
| `RiskEvaluationRequested` | 54 | 54 | 0 |
| `RiskAlertRaisedEvent` | 0 | 0 | 0 |

Для короткого локального run это хороший baseline: events не потеряны, quote channel не дропал сообщения, trade write path имеет измеримую latency, Risk Engine успел обработать накопившиеся requests.

## Почему active alerts = 0

Это ожидаемо.

Сценарий использует маленькие объемы сделок:

```text
0.1 - 0.5 lot
```

Seed rule `MaxExposureLimit` срабатывает только при заметно большем exposure. Цель текущего load run - не создать alert storm, а проверить pipeline под потоком котировок и сделок.

Позже можно добавить отдельный профиль:

```text
AlertStorm
```

Он будет намеренно открывать большие позиции и проверять deduplication/active-alert behavior.

## Что изменилось в репозитории

- Добавлен `LoadTestWorker`.
- Добавлен `LoadTestScenarioRunner`.
- Добавлены `LoadTestOptions`, `LoadTestProfile`, validator и settings.
- Добавлен `MarkdownLoadTestReportWriter`.
- Добавлен `IRiskAlertRepository.CountActiveAsync(...)`.
- `appsettings.json` получил секцию `LoadTest`.
- Добавлены unit-тесты validator/report writer.
- Добавлен `docs/load-tests/README.md`.
- Сохранен локальный load report `2026-07-02-local-load-report.md`.
- Обновлены `README.md`, `ARCHITECTURE.md`, `DEVELOPMENT_PLAN.md`, `book/progress.md`.

## Альтернативы

- **HTTP load test сразу**: реалистичнее для API, но смешивает backend pipeline и HTTP overhead. Добавим позже.
- **Прямой insert в БД**: быстрее генерирует данные, но не проверяет handlers, transactions и events.
- **Отдельный console runner**: удобно для CI, но для первой итерации hosted worker проще встроить в текущий composition root.
- **k6/Gatling/JMeter**: хороши для внешней нагрузки, но требуют готового Docker Compose/API окружения.
- **Отдельная read model для метрик**: преждевременно; channel snapshots уже дают нужный baseline.

## Частые ошибки

- Включить load worker по умолчанию.
- Писать сделки напрямую в таблицу `trades` и считать это проверкой backend-а.
- Считать channel depth сразу после генерации и забыть про internal batch writer state.
- Мерить только average latency и не смотреть p95.
- Не фиксировать параметры запуска рядом с отчетом.
- Сравнивать отчеты, снятые на разных профилях или разных машинах, как равные.
- Считать локальный результат production SLA.

## Вопросы для интервью

- Почему load scenario идет через application handlers, а не через прямой SQL?
- Почему `LoadTestWorker` выключен по умолчанию?
- Что измеряет trade latency в этом отчете?
- Почему risk evaluation latency не равна latency `CreateTradeHandler`?
- Как понять, что quote channel начал испытывать pressure?
- Почему важно ждать drain window?
- Что изменится при переходе от in-process load runner к k6?
- Какие профили нагрузки стоит добавить следующими?

## Проверка результата

Команда:

```bash
dotnet test PulseRisk.slnx
```

Результат текущей итерации:

- unit-тесты: `73/73`;
- integration tests: `6/9`;
- skipped integration tests: `3/9`.

Три skipped tests зависят от Docker/Testcontainers PostgreSQL. Для локального load report использовался отдельный disposable PostgreSQL 18 cluster.

## Чек-лист главы

- [x] `LoadTestWorker` добавлен.
- [x] `LoadTestOptions` и validator добавлены.
- [x] Профили `Quotes500`, `Quotes1000`, `Quotes5000` добавлены.
- [x] Генерация клиентов добавлена.
- [x] Генерация счетов добавлена.
- [x] Генерация сделок добавлена.
- [x] Генерация котировок добавлена.
- [x] Throughput и latency считаются.
- [x] Channel deltas собираются через `IEventChannelMonitor`.
- [x] Active alerts считаются через repository.
- [x] Markdown report writer добавлен.
- [x] Локальный отчет сохранен в `docs/load-tests`.
- [x] Документация обновлена.
- [x] Проверки выполнены.
