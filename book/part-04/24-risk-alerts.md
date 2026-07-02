# Глава 24. Создаем risk alerts и deduplication

## Цель главы

Научить Risk Engine не только вычислять нарушение risk rule, но и сохранять результат как `RiskAlert`.

После главы 23 у нас уже есть:

- `RiskEvaluationContext`;
- `IRiskRuleStrategy`;
- стратегии `MaxExposure`, `MaxLoss`, `MarginLevelWarning`;
- `RiskRuleEvaluationResult`;
- `RiskEvaluationProcessor`, который применяет стратегии.

Но пока нарушение только логировалось. Для backend-системы risk-management этого недостаточно: alert должен стать доменным фактом, который можно сохранить, прочитать, показать в UI, отправить downstream-потребителю и не создавать бесконечно заново на каждый tick.

## Демонстрируемые компетенции

Глава показывает:

- создание доменного объекта через factory;
- разделение domain logic, application ports и infrastructure adapters;
- active-alert deduplication;
- unit of work для сохранения нескольких alert-ов одним commit;
- публикацию internal event после успешного сохранения;
- применение GoF/GRASP без overengineering.

## Что сделаем сейчас

Добавим:

- `RiskAlertFactory`;
- application-port `IRiskAlertRepository`;
- EF Core adapter `EfRiskAlertRepository`;
- подключение repository в DI;
- сохранение alert-ов в `RiskEvaluationProcessor`;
- active-alert deduplication;
- публикацию `RiskAlertRaisedEvent`;
- unit-тесты фабрики;
- unit-тесты processor-а для создания, deduplication и нескольких alert-ов в одной evaluation.

## Проектное решение

Risk rule strategy по-прежнему не создает `RiskAlert` напрямую.

Она возвращает только результат проверки:

```csharp
RiskRuleEvaluationResult.Triggered(
    RiskAlertType.ExposureLimitExceeded,
    rule.Severity,
    "...");
```

Дальше `RiskEvaluationProcessor` выполняет orchestration:

1. Получает triggered result.
2. Просит `RiskAlertFactory` создать доменный alert.
3. Проверяет, нет ли уже active alert с таким же ключом.
4. Добавляет новый alert в repository.
5. Сохраняет изменения через `IUnitOfWork`.
6. Публикует `RiskAlertRaisedEvent`.

Ключ deduplication в первой версии:

```text
client_id + trading_account_id + symbol + alert_type + resolved_at IS NULL
```

Это означает: пока alert активен, повторное срабатывание того же типа по тому же счету и символу не создает новый alert.

## Паттерны GoF/GRASP

### Simple Factory

`RiskAlertFactory` централизует создание `RiskAlert`.

Фабрика решает проблему неслучайного создания:

- alert можно создать только из triggered result;
- `AlertType`, `Severity`, `Message` должны быть заполнены;
- `RiskAlert` создается active, то есть с `ResolvedAt = null`;
- timestamp берется из текущей evaluation.

Это не GoF Factory Method в строгом смысле, потому что нет иерархии creator-классов и polymorphic factory method. Поэтому в книге честно называем решение Simple Factory.

### GRASP Creator

`RiskAlertFactory` подходит под GRASP Creator: она имеет все данные, необходимые для сборки `RiskAlert`, и защищает инвариант "alert создается в валидном состоянии".

Мы не помещаем создание в strategy, потому что strategy отвечает за detection logic. И мы не размазываем `new RiskAlert(...)` по processor-у, потому что processor отвечает за orchestration.

### GRASP Indirection

`IRiskAlertRepository` отделяет Risk Engine от EF Core.

Processor не знает:

- какие таблицы используются;
- как настроен partial index;
- как EF Core конвертирует `Symbol`;
- как именно проверяется active alert.

Он знает только application-level контракт.

### Protected Variations

Если позже deduplication изменится с active-alert lookup на `deduplication_key`, cooldown-window или update existing alert, processor можно будет менять ограниченно, а EF-детали останутся в adapter-е.

### Low Coupling и High Cohesion

Фабрика занимается созданием alert-а.

Repository занимается persistence.

Processor занимается workflow.

Strategy занимается проверкой конкретного правила.

Каждый компонент маленький и тестируемый.

## Реализация шаг за шагом

### Шаг 1. RiskAlertFactory

Фабрика находится в Domain layer:

```csharp
public sealed class RiskAlertFactory
{
    public RiskAlert Create(
        RiskEvaluationContext context,
        RiskRuleEvaluationResult result,
        DateTimeOffset createdAt)
    {
        ...
    }
}
```

Она выбрасывает `ArgumentException`, если result не triggered или содержит неполные данные.

Это полезно, потому что `RiskRuleEvaluationResult` технически допускает nullable-поля для состояния `NotTriggered`, а `RiskAlert` не должен создаваться из такого состояния.

### Шаг 2. IRiskAlertRepository

Application layer получает новый port:

```csharp
public interface IRiskAlertRepository
{
    Task<bool> ExistsActiveAsync(
        Guid clientId,
        Guid tradingAccountId,
        Symbol symbol,
        RiskAlertType alertType,
        CancellationToken cancellationToken);

    Task AddAsync(RiskAlert alert, CancellationToken cancellationToken);
}
```

Контракт намеренно маленький.

Для главы 24 нам не нужен полный CRUD repository. Нам нужны две операции:

- проверить active duplicate;
- добавить новый alert.

### Шаг 3. EfRiskAlertRepository

Infrastructure adapter реализует поиск active alert:

```csharp
alert.ClientId == clientId
&& alert.TradingAccountId == tradingAccountId
&& alert.Symbol == symbol
&& alert.AlertType == alertType
&& alert.ResolvedAt == null
```

Запрос read-only, поэтому используется `AsNoTracking()`.

Схема БД уже содержит partial index на active alerts, поэтому запрос соответствует заранее спроектированной модели.

### Шаг 4. Подключаем DI

В Application регистрируется `RiskAlertFactory`.

В Infrastructure регистрируется `IRiskAlertRepository -> EfRiskAlertRepository`.

`RiskEvaluationProcessor` получает новые зависимости через constructor injection.

### Шаг 5. Сохраняем alert в processor-е

Когда rule triggered:

```csharp
var alert = riskAlertFactory.Create(context, result, request.RequestedAt);
```

Дальше processor проверяет два уровня deduplication.

Первый уровень - локальный ключ текущей evaluation:

```csharp
var evaluationAlertKeys = new HashSet<(Guid, Guid, string, RiskAlertType)>();
```

Он нужен, если два active rules создают одинаковый `RiskAlertType` в одном проходе до `SaveChanges`.

Второй уровень - active alert в БД:

```csharp
await riskAlerts.ExistsActiveAsync(...);
```

Он нужен, если alert уже был создан предыдущей evaluation и еще не resolved.

### Шаг 6. Сохраняем и публикуем событие

Processor сохраняет все новые alert-ы одним commit:

```csharp
await unitOfWork.SaveChangesAsync(cancellationToken);
```

И только после этого публикует:

```csharp
RiskAlertRaisedEvent
```

Это важный порядок. Если опубликовать событие до commit, downstream-потребитель может получить `AlertId`, которого еще нет в БД.

## Deduplication и cooldown

В полноценной системе можно встретить несколько моделей:

- active-alert deduplication;
- explicit `deduplication_key`;
- cooldown по rule или alert type;
- update existing alert вместо создания нового;
- auto-resolve, когда метрика вернулась в норму.

В этой итерации выбран active-alert подход.

Он достаточно силен для MVP:

- не создает лавину дублей;
- не требует новой таблицы;
- опирается на уже существующее поле `ResolvedAt`;
- хорошо объясняется на интервью;
- оставляет место для future enhancement.

Это еще не полноценный cooldown по времени. Скорее это state-based cooldown: пока alert активен, такой же alert не создается повторно.

## Почему не создаем alert внутри strategy

Strategy отвечает на вопрос:

```text
Правило нарушено или нет?
```

Factory отвечает на вопрос:

```text
Как из triggered result сделать валидный RiskAlert?
```

Processor отвечает на вопрос:

```text
Когда сохранить alert и когда публиковать event?
```

Если strategy начнет сохранять alert, она узнает про repository, unit of work и event channel. Это сломает low coupling и сделает доменное правило тяжелым для unit-тестов.

## Проверка результата

Добавлены unit-тесты:

- `RiskAlertFactoryTests`;
- `RiskEvaluationProcessorTests`.

Processor tests проверяют:

- triggered rule создает alert и публикует event;
- existing active alert подавляет duplicate;
- несколько разных rules могут создать несколько alert-ов в одной evaluation;
- два rules одного alert type создают только один alert.

Команда:

```bash
dotnet test PulseRisk.slnx
```

Результат текущей итерации:

- unit-тесты: `59/59`;
- integration tests: `6/8`;
- skipped integration tests: `2/8`.

Оба skipped tests зависят от Docker/Testcontainers PostgreSQL.

## Что изменилось в репозитории

- Добавлен `RiskAlertFactory`.
- Добавлен `IRiskAlertRepository`.
- Добавлен `EfRiskAlertRepository`.
- `RiskEvaluationProcessor` создает и сохраняет alert-ы.
- Добавлен active-alert deduplication.
- Добавлена публикация `RiskAlertRaisedEvent`.
- DI обновлен для Application и Infrastructure.
- Добавлены unit-тесты фабрики и processor-а.
- Обновлены `DEVELOPMENT_PLAN.md`, `ARCHITECTURE.md`, `README.md`, `book/progress.md`.

## Альтернативы

- **Сразу добавить `deduplication_key` в таблицу**: сильнее для concurrent scenarios, но требует миграции и пока усложняет модель.
- **Сохранять каждый alert отдельным commit**: проще локально, хуже для batch evaluation и consistency.
- **Публиковать event до commit**: быстрее, но небезопасно.
- **Хранить cooldown seconds в `RiskRule`**: полезно позже, но сейчас в entity нет lifecycle для rule-specific cooldown.
- **Обновлять существующий alert**: хорошо для счетчиков повторов, но требует дополнительных полей.

## Частые ошибки

- Создавать alert из `NotTriggered` result.
- Дублировать active alert на каждый tick.
- Публиковать event до сохранения в БД.
- Делать strategy зависимой от EF Core.
- Вызывать `SaveChanges` внутри repository.
- Проверять dedup только в памяти и забыть про существующие active alerts.

## Вопросы для интервью

- Почему `RiskAlertFactory` здесь Simple Factory, а не GoF Factory Method?
- Какой GRASP-принцип объясняет вынесение создания alert-а в factory?
- Почему event публикуется после `SaveChanges`?
- Как active-alert deduplication отличается от time-based cooldown?
- Что изменится, если добавить `deduplication_key`?
- Где лучше реализовать auto-resolve alert-а?
- Почему repository не вызывает `SaveChanges` самостоятельно?

## Чек-лист главы

- [x] `RiskAlertFactory` добавлен.
- [x] `IRiskAlertRepository` добавлен.
- [x] `EfRiskAlertRepository` добавлен.
- [x] Processor создает alert через factory.
- [x] Processor проверяет active duplicate в repository.
- [x] Processor подавляет duplicate внутри одной evaluation.
- [x] Processor сохраняет alert через `IUnitOfWork`.
- [x] Processor публикует `RiskAlertRaisedEvent` после commit.
- [x] Unit-тесты фабрики добавлены.
- [x] Unit-тесты processor-а добавлены.
- [x] Код собирается.
- [x] Проверки выполнены.
