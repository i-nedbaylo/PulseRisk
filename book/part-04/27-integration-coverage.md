# Глава 27. Укрепляем integration tests

## Цель главы

Проверить, что ключевые части системы работают вместе на настоящей PostgreSQL, а не только изолированно в unit-тестах.

После главы 26 бизнес-логика Risk Engine хорошо покрыта unit-тестами. Но unit-тесты не отвечают на вопросы:

- применяются ли EF Core migrations;
- корректно ли работают mappings value objects и enum-ов;
- запускаются ли background workers в test host-е;
- проходит ли событие от HTTP API до `RiskAlert` в PostgreSQL;
- не сломались ли DI-регистрации между слоями.

Для этого нужны integration tests.

## Демонстрируемые компетенции

Глава показывает:

- Testcontainers PostgreSQL;
- `WebApplicationFactory`;
- применение EF Core migrations в тестовой базе;
- HTTP-level setup через публичный API;
- проверку persistence boundary через `PulseRiskDbContext`;
- polling асинхронного background flow;
- test data builder без сокрытия assertions;
- стабильное skip-поведение, если Docker Engine недоступен.

## Что уже есть

В проекте уже были integration tests:

- health endpoint;
- Swagger endpoint;
- EF Core model mapping tests;
- PostgreSQL scenario `client -> account -> trade -> position`;
- PostgreSQL scenario batch insert quotes.

В этой главе добавляем самый важный risk-management сценарий:

```text
latest quote exists
-> client/account created through HTTP API
-> trade creates large position
-> PositionChangedEvent is published
-> RiskEvaluationRequested is processed
-> MaxExposureRuleStrategy triggers
-> RiskAlert is persisted in PostgreSQL
```

## Паттерны GoF/GRASP и тестовая архитектура

### Adapter

`WebApplicationFactory<Program>` адаптирует ASP.NET Core приложение к тестовому `HttpClient`.

Тест не вызывает handlers напрямую. Он проходит через реальный HTTP boundary.

### Template Method

`PulseRiskPostgresApplicationFactory.ConfigureWebHost(...)` переопределяет часть lifecycle test host-а: окружение и connection string.

Это Template Method в инфраструктурном смысле: framework задает skeleton, тест меняет только нужный шаг.

### Builder

`IntegrationTestDataBuilder` создает повторяемые данные через API:

- client;
- trading account;
- trade.

Это не builder доменной модели. Это test data builder для HTTP-сценариев.

Он уменьшает шум в тестах, но assertions остаются в самих тестах.

### Indirection

Test host получает connection string через in-memory configuration. Production configuration не меняется.

### Protected Variations

Тесты зависят от публичного API и persistence boundary, а не от деталей конкретного handler-а или worker-а.

## Реализация шаг за шагом

### Шаг 1. Добавляем IntegrationTestDataBuilder

В `tests/PulseRisk.IntegrationTests/TestInfrastructure` появился helper:

```csharp
internal sealed class IntegrationTestDataBuilder(HttpClient client)
```

Он умеет:

- `CreateClientAsync`;
- `CreateTradingAccountAsync`;
- `CreateTradeAsync`.

Все операции идут через HTTP API, а не через прямую вставку в БД.

Так integration test сохраняет смысл: проверяется реальный API/application/infrastructure path.

### Шаг 2. Обновляем существующий trade-processing тест

Старый тест `CreateTrade_ShouldPersistTradeAndUpdatePosition` использовал локальный `PostAsync`.

Теперь он использует `IntegrationTestDataBuilder`.

Поведение теста не изменилось, но setup стал переиспользуемым для следующих сценариев.

### Шаг 3. Готовим latest quote

Risk Engine не может считать floating PnL без latest quote.

Перед созданием сделки тест сохраняет котировку:

```csharp
await quoteWriter.WriteAsync(
    [new QuoteTick("EURUSD", Bid: 1.30m, Ask: 1.31m, quoteTimestamp)],
    CancellationToken.None);
```

Для этого используется `IQuoteBatchWriter`.

Это setup шага, которого пока нет в публичном API. Он не подменяет проверяемый risk flow, а только создает необходимое рыночное состояние.

### Шаг 4. Создаем позицию через HTTP API

Тест создает:

- client;
- trading account;
- trade на `10` лотов `EURUSD` по цене `1.25`.

Seed rule `MaxExposureLimit` имеет threshold `1_000_000`.

Позиция:

```text
10 * 1.25 * 100_000 = 1_250_000
```

Значит, exposure превышает threshold.

### Шаг 5. Ждем background processing

После `POST /api/trades` обработка alert-а происходит асинхронно:

```text
CreateTradeHandler
-> PositionChangedEvent channel
-> PositionChangedEventWorker
-> RiskEvaluationRequested channel
-> RiskEvaluationRequestedWorker
-> RiskEvaluationProcessor
-> RiskAlertRepository
```

Нельзя сразу читать `risk_alerts` и ожидать запись синхронно.

Поэтому тест использует polling:

```csharp
WaitForRiskAlertAsync(...)
```

Он несколько секунд проверяет PostgreSQL и завершает ожидание сразу, когда alert найден.

Это лучше фиксированного `Task.Delay(5000)`: тест не тормозит, если worker сработал быстро, и не становится случайным из-за одного жесткого ожидания.

### Шаг 6. Проверяем persisted alert

Тест проверяет:

- `ClientId`;
- `TradingAccountId`;
- `Symbol`;
- `AlertType = ExposureLimitExceeded`;
- `Severity = Critical`;
- message содержит `Net exposure`;
- `ResolvedAt = null`;
- `IsActive = true`.

Это доказывает, что прошел не только расчет, но и persistence результата.

## Почему этот тест integration-level

Сценарий затрагивает:

- HTTP API;
- validators;
- handlers;
- EF Core repositories;
- migrations;
- PostgreSQL value conversions;
- in-memory event channels;
- background workers;
- Risk Engine;
- risk alert persistence.

Проверять это unit-тестами можно только по частям. Но цель главы - убедиться, что части действительно соединены.

## Skip-поведение без Docker

PostgreSQL integration tests используют `SkippableFact`.

Если Docker Engine недоступен, тесты не падают красным:

```text
Docker is not available; skipping PostgreSQL Testcontainers scenario.
```

Это удобно для локальной разработки на машине без Docker. В CI Docker должен быть доступен, и такие тесты должны выполняться.

## Проверка результата

Команда:

```bash
dotnet test PulseRisk.slnx
```

Результат текущей итерации в локальной среде без Docker:

- unit-тесты: `69/69`;
- integration tests: `6/9`;
- skipped integration tests: `3/9`.

Три skipped tests зависят от Docker/Testcontainers PostgreSQL:

- trade processing PostgreSQL scenario;
- quote batch writer PostgreSQL scenario;
- risk alert PostgreSQL scenario.

## Что изменилось в репозитории

- Добавлен `IntegrationTestDataBuilder`.
- Обновлен `TradeProcessingPostgreSqlTests`.
- Добавлен `RiskAlertPostgreSqlTests`.
- Обновлены `DEVELOPMENT_PLAN.md`, `ARCHITECTURE.md`, `README.md`, `book/progress.md`.

## Альтернативы

- **Вызвать `RiskEvaluationProcessor` напрямую**: проще, но это был бы application/infrastructure test, а не сквозной scenario.
- **Вставить trade напрямую в БД**: быстрее, но обходит API, validation и handler.
- **Проверять alert через event channel**: полезно позже, но persistence важнее для текущего сценария.
- **Фиксированный delay вместо polling**: проще написать, но более flaky и медленнее.
- **Один общий container на весь test class**: быстрее, но сильнее связывает тесты состоянием.

## Частые ошибки

- Не засеять latest quote перед risk evaluation.
- Проверять alert сразу после HTTP response без ожидания background worker-а.
- Скрыть assertions внутри test data builder.
- Держать один `DbContext` во время polling и читать stale state.
- Считать skipped Testcontainers tests достаточными для CI.

## Вопросы для интервью

- Почему risk alert scenario нужен как integration test?
- Почему setup идет через HTTP API, а quote seed - через repository port?
- Почему polling лучше фиксированного delay?
- Что должен делать CI, если Docker недоступен?
- Какие части системы покрывает этот scenario?
- Какие integration tests нужно добавить следующими?

## Чек-лист главы

- [x] `IntegrationTestDataBuilder` добавлен.
- [x] Trade processing integration test использует builder.
- [x] Risk alert PostgreSQL scenario добавлен.
- [x] Тест засевает latest quote.
- [x] Тест создает client/account/trade через HTTP API.
- [x] Тест ждет background risk processing через polling.
- [x] Тест проверяет persisted active `RiskAlert`.
- [x] План и документация обновлены.
- [x] Проверки выполнены.
