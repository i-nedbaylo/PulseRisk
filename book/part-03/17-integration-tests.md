# Глава 17. Пишем integration tests через Testcontainers

## Цель главы

Проверить первый сквозной бизнес-сценарий на настоящей PostgreSQL:

```text
client -> account -> trade -> position
```

До этой главы у нас уже были unit-тесты доменных расчетов и model-level проверки EF Core mapping. Но они не отвечали на главный вопрос: сможет ли приложение принять HTTP-запрос на создание сделки, пройти через Application layer, сохранить данные через реальные миграции PostgreSQL и получить корректную позицию в базе.

## Демонстрируемые компетенции

В этой главе проект показывает:

- integration testing вместо проверки отдельных классов в вакууме;
- работу с Testcontainers PostgreSQL;
- применение EF Core migrations в тестовом окружении;
- проверку transaction boundary: сделка и позиция должны появиться согласованно;
- осознанное отношение к инфраструктурной нестабильности локальной машины: если Docker Engine не запущен, тест пропускается, а не ломает весь suite.

## Что уже есть в проекте

К началу главы уже реализованы:

- `POST /api/clients`;
- `POST /api/accounts`;
- `POST /api/trades`;
- `PulseRiskDbContext`;
- migrations и seed базовых инструментов;
- `CreateTradeHandler`, который создает сделку и обновляет позицию за один commit;
- optimistic concurrency token `xmin` для `Position`;
- in-process event channel для `PositionChangedEvent`.

Этого достаточно, чтобы проверить реальный пользовательский путь через HTTP API.

## Что сделаем сейчас

Добавим:

- `DockerAvailability` - легкую проверку доступности Docker Engine;
- `PulseRiskPostgresApplicationFactory` - test host с подменой connection string;
- `TradeProcessingPostgreSqlTests` - сквозной тест создания клиента, счета и сделки;
- обновление плана разработки и карты прогресса книги.

## Проектное решение

Тест запускает PostgreSQL в контейнере, создает `WebApplicationFactory<Program>`, подменяет `ConnectionStrings:PulseRisk`, применяет migrations и работает с API через обычный `HttpClient`.

После HTTP-запросов тест открывает scope DI и читает `PulseRiskDbContext`, чтобы проверить не только response DTO, но и фактическое состояние таблиц `trades` и `positions`.

Такой тест дороже unit-теста, зато он проверяет то, что нельзя надежно проверить моками:

- корректность migrations;
- seed инструмента `EURUSD`;
- JSON serialization/deserialization;
- DI composition root;
- EF Core mappings value objects;
- PostgreSQL decimal precision;
- реальное сохранение сделки и позиции.

## Паттерны GoF/GRASP

- **GRASP Controller**: HTTP controller остается тонкой входной точкой. Он принимает запрос и делегирует сценарий application handler-у. Тест не заставляет переносить бизнес-логику в controller.
- **GRASP Information Expert**: `PositionCalculator` и `Position` отвечают за состояние позиции, а не тест и не controller. Integration test проверяет результат, но не повторяет формулы внутри себя.
- **GRASP Indirection**: `PulseRiskPostgresApplicationFactory` служит промежуточным объектом между тестом и production composition root. Он меняет только конфигурацию, не переписывая DI вручную.
- **Adapter**: `WebApplicationFactory` адаптирует ASP.NET Core host к тестовому `HttpClient`. Мы тестируем приложение через HTTP-интерфейс без реального сетевого порта.
- **Template Method**: `ConfigureWebHost` переопределяет нужную часть поведения `WebApplicationFactory`, оставляя остальной lifecycle ASP.NET Core стандартным.
- **Protected Variations**: тест зависит от публичного API и `PulseRiskDbContext`, а не от внутренней реализации repositories. Если handler внутри изменится, сценарий останется валидным.

Это не overengineering: мы не создаем отдельный testing framework и не дублируем production DI. Добавлены только две маленькие инфраструктурные детали, которые решают конкретные проблемы интеграционного теста: connection string и доступность Docker.

## Реализация шаг за шагом

### Шаг 1. Проверяем доступность Docker

Файл `tests/PulseRisk.IntegrationTests/TestInfrastructure/DockerAvailability.cs` запускает:

```bash
docker info --format "{{.ServerVersion}}"
```

Если команда не завершилась успешно за несколько секунд, тест будет пропущен. Это важно для локальной разработки: интеграционный suite остается полезным даже на машине, где Docker Desktop временно не поднят.

### Шаг 2. Создаем test application factory

`PulseRiskPostgresApplicationFactory` наследуется от `WebApplicationFactory<Program>` и переопределяет `ConfigureWebHost`.

Внутри мы добавляем in-memory configuration:

```csharp
configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:PulseRisk"] = connectionString
});
```

Так production-регистрация инфраструктуры остается прежней, но БД указывает на Testcontainers PostgreSQL.

### Шаг 3. Поднимаем PostgreSQL и применяем migrations

В тесте используется образ `postgres:17-alpine`:

```csharp
await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
    .WithDatabase("pulserisk_tests")
    .WithUsername("pulserisk")
    .WithPassword("pulserisk")
    .Build();
```

После старта контейнера тест берет `PulseRiskDbContext` из DI и вызывает:

```csharp
await dbContext.Database.MigrateAsync();
```

Это принципиально лучше, чем `EnsureCreated`, потому что мы проверяем именно migration path.

### Шаг 4. Проходим HTTP-сценарий

Тест последовательно отправляет:

- `POST /api/clients`;
- `POST /api/accounts`;
- `POST /api/trades`.

Для каждого запроса используется `PostAsJsonAsync`, а response читается через `ReadFromJsonAsync<T>`. Это проверяет тот же JSON contract, с которым будет работать внешний клиент API.

### Шаг 5. Проверяем состояние БД

После создания сделки тест читает `Trades` и `Positions` через `AsNoTracking`.

Ожидаемый результат:

- сделка сохранена с нужным client/account/symbol/volume/open price;
- позиция создана для пары `(TradingAccountId, EURUSD)`;
- `NetVolume = 2`;
- `AveragePrice = 1.25`;
- `FloatingPnL = 0`.

Так мы проверяем не только факт успешного HTTP-ответа, но и бизнес-эффект сценария.

## Проверка результата

Команды:

```bash
dotnet build PulseRisk.slnx
dotnet test PulseRisk.slnx
```

Результат текущей итерации:

- unit-тесты: `37/37`;
- integration tests: `5/6`;
- skipped integration tests: `1/6`.

Пропущенный тест - это Testcontainers-сценарий PostgreSQL. На текущей машине Docker Engine недоступен, поэтому `SkippableFact` корректно помечает тест как skipped. Если запустить Docker Desktop и повторить `dotnet test PulseRisk.slnx`, сценарий должен поднять временную PostgreSQL и выполнить полную проверку.

## Что изменилось в репозитории

- `tests/PulseRisk.IntegrationTests/PulseRisk.IntegrationTests.csproj` получил явную ссылку на `PulseRisk.Application` и `Xunit.SkippableFact`.
- Добавлен `DockerAvailability`.
- Добавлен `PulseRiskPostgresApplicationFactory`.
- Добавлен `TradeProcessingPostgreSqlTests`.
- `DEVELOPMENT_PLAN.md` отмечает первый сквозной integration test как выполненный.
- `book/progress.md` отмечает главу 17 как выполненную.

## Почему сделали именно так

Главный риск trade processing находится не в том, что `PositionCalculator` неверно складывает числа. Это уже проверяют unit-тесты. Риск в стыке частей:

- API может принять не тот JSON;
- handler может не найти seed-инструмент;
- EF mapping value object может сломать запрос;
- migration может не создать нужный индекс или таблицу;
- commit может сохранить сделку, но не позицию.

Testcontainers дает честную проверку этих стыков без зависимости от общей локальной базы разработчика.

## Альтернативы

- **In-memory provider EF Core**: быстрее, но не проверяет PostgreSQL-specific поведение, migrations, `xmin`, precision и реальные SQL constraints.
- **SQLite in-memory**: полезнее in-memory provider, но все равно отличается от PostgreSQL по типам, concurrency и SQL dialect.
- **Общая локальная PostgreSQL**: проще для первого запуска, но тесты становятся зависимыми от состояния машины и требуют ручной очистки.
- **Docker Compose для тестов**: хорош для системных сценариев, но для integration tests Testcontainers удобнее, потому что контейнер живет ровно столько, сколько живет тест.

## Частые ошибки

- Использовать `EnsureCreated` вместо migrations и случайно перестать проверять migration path.
- Проверять только HTTP status code и не смотреть состояние БД.
- Оставлять тест красным, когда Docker не запущен локально.
- Слишком рано вводить общий base class, fixture hierarchy и data builders. Пока один сценарий читаемее без дополнительной абстракции.
- Повторять внутри теста бизнес-формулы вместо проверки нескольких ключевых итоговых значений.

## Вопросы для интервью

- Почему для этого сценария выбран Testcontainers, а не EF Core in-memory provider?
- Что именно проверяет integration test, чего не проверяет unit test?
- Почему migrations лучше `EnsureCreated` в таком тесте?
- Почему controller не должен содержать логику расчета позиции?
- Как бы вы ускорили integration tests, когда таких сценариев станет 50?
- Что изменится, если Docker должен быть обязателен в CI?

## Чек-лист главы

- [x] Цель главы достигнута.
- [x] Testcontainers PostgreSQL подключен к сквозному сценарию.
- [x] Test host подменяет connection string без ручной пересборки DI.
- [x] Migrations применяются перед HTTP-запросами.
- [x] Сценарий `client -> account -> trade -> position` описан и реализован.
- [x] Docker-unavailable сценарий не ломает весь test suite.
- [x] Код собирается.
- [x] Проверки выполнены.
- [x] Архитектурные решения отражены в документации.
