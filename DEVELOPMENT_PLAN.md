# PulseRisk: детальный план разработки

План ориентирован на учебно-демонстрационный проект под вакансию C# (.NET) Developer Middle+ / Senior в IndigoSoft. Главный акцент - backend-ядро real-time risk-management системы: конкурентная обработка событий, PostgreSQL, производительность, тестируемость и понятная архитектура без overengineering.

## 0. Подготовка репозитория

- [x] Зафиксировать название проекта: `PulseRisk`.
- [x] Создать solution `PulseRisk.slnx`.
- [x] Создать структуру каталогов:
  - [x] `src/PulseRisk.Api`;
  - [x] `src/PulseRisk.Application`;
  - [x] `src/PulseRisk.Domain`;
  - [x] `src/PulseRisk.Infrastructure`;
  - [x] `src/PulseRisk.BackgroundWorkers`;
  - [x] `tests/PulseRisk.UnitTests`;
  - [x] `tests/PulseRisk.IntegrationTests`;
  - [x] `tests/PulseRisk.Benchmarks`;
  - [x] `docs/explain-analyze`;
  - [x] `docs/load-tests`.
- [x] Настроить `.editorconfig`.
- [x] Настроить `Directory.Build.props`.
- [x] Включить nullable reference types.
- [x] Включить implicit usings.
- [x] Выбрать единый стиль warnings-as-errors для production-проектов.
- [x] Добавить базовый `.gitignore` для .NET, Rider/Visual Studio, Docker и test artifacts.

## 1. Базовые зависимости и конфигурация

- [x] Подключить ASP.NET Core Web API на .NET 10.
- [x] Подключить Swagger/OpenAPI.
- [x] Подключить EF Core provider для PostgreSQL.
- [x] Подключить Dapper для оптимизированных read-only запросов.
- [x] Подключить FluentValidation.
- [x] Подключить Serilog или настроить structured logging стандартными средствами.
- [x] Подключить xUnit или NUnit.
- [x] Подключить FluentAssertions.
- [x] Подключить Testcontainers PostgreSQL для integration tests.
- [x] Подключить BenchmarkDotNet для benchmark-проекта.
- [ ] Создать options-классы:
  - [ ] `MarketDataOptions`;
  - [ ] `RiskEngineOptions`;
  - [ ] `ChannelOptions`;
  - [ ] `QuoteBatchOptions`;
  - [ ] `LoadTestOptions`.
- [ ] Настроить validation options на старте приложения.

## 2. Domain layer

- [ ] Создать enums:
  - [ ] `ClientStatus`;
  - [ ] `TradeSide`;
  - [ ] `RiskRuleType`;
  - [ ] `RiskAlertType`;
  - [ ] `RiskSeverity`;
  - [ ] `CurrencyCode`.
- [ ] Создать value objects:
  - [ ] `Symbol`;
  - [ ] `Money`;
  - [ ] `Price`;
  - [ ] `Volume`;
  - [ ] `Percentage`.
- [ ] Реализовать entity `Client`.
- [ ] Реализовать entity `TradingAccount`.
- [ ] Реализовать entity `Instrument`.
- [ ] Реализовать entity `Trade`.
- [ ] Реализовать entity `Position`.
- [ ] Реализовать entity `Quote`.
- [ ] Реализовать entity `RiskRule`.
- [ ] Реализовать entity `RiskAlert`.
- [ ] Добавить доменные инварианты:
  - [ ] volume > 0;
  - [ ] price > 0;
  - [ ] bid <= ask;
  - [ ] leverage > 0;
  - [ ] inactive instrument нельзя использовать для новой сделки;
  - [ ] disabled risk rule не участвует в проверке.
- [ ] Реализовать доменный сервис `PositionCalculator`.
- [ ] Реализовать доменный сервис `PnLCalculator`.
- [ ] Реализовать модели risk evaluation:
  - [ ] `RiskEvaluationContext`;
  - [ ] `RiskMetricSnapshot`;
  - [ ] `RiskRuleEvaluationResult`.

## 3. Risk rules

- [ ] Создать интерфейс `IRiskRuleStrategy`.
- [ ] Реализовать resolver `RiskRuleStrategyResolver`.
- [ ] Реализовать `MaxExposureRuleStrategy`.
- [ ] Реализовать `MaxLossRuleStrategy`.
- [ ] Реализовать `MarginLevelWarningRuleStrategy`.
- [ ] Реализовать `PriceSpikeDetectionRuleStrategy`.
- [ ] Реализовать `HighFrequencyTradingActivityRuleStrategy`.
- [ ] Создать `RiskAlertFactory`.
- [ ] Добавить cooldown/deduplication модель для алертов.
- [ ] Покрыть каждое правило unit-тестами.
- [ ] Проверить сценарий: rule disabled не создает alert.
- [ ] Проверить сценарий: несколько rules могут создать несколько alert'ов в одной evaluation.

## 4. Infrastructure: PostgreSQL и EF Core

- [ ] Создать `PulseRiskDbContext`.
- [ ] Настроить mappings через `IEntityTypeConfiguration<T>`.
- [ ] Настроить snake_case naming convention.
- [ ] Настроить precision для денежных и ценовых полей.
- [ ] Настроить enum conversion.
- [ ] Настроить `xmin`/concurrency token для `Position`, если выбран optimistic concurrency.
- [ ] Создать initial migration.
- [ ] Создать таблицу `clients`.
- [ ] Создать таблицу `trading_accounts`.
- [ ] Создать таблицу `instruments`.
- [ ] Создать таблицу `trades`.
- [ ] Создать таблицу `positions`.
- [ ] Создать таблицу `quotes`.
- [ ] Создать таблицу `risk_rules`.
- [ ] Создать таблицу `risk_alerts`.
- [ ] Добавить обязательные индексы:
  - [ ] `trades(client_id, created_at DESC)`;
  - [ ] `trades(symbol, created_at DESC)`;
  - [ ] `positions(client_id, symbol)`;
  - [ ] unique `positions(trading_account_id, symbol)`;
  - [ ] `quotes(symbol, timestamp DESC)`;
  - [ ] `risk_alerts(client_id, created_at DESC)`;
  - [ ] `risk_alerts(severity, created_at DESC)`.
- [ ] Добавить partial index для активных алертов.
- [ ] Добавить seed базовых инструментов:
  - [ ] `EURUSD`;
  - [ ] `GBPUSD`;
  - [ ] `XAUUSD`.
- [ ] Добавить seed базовых risk rules.

## 5. Infrastructure: repositories и query objects

- [ ] Создать интерфейсы repositories в Application layer.
- [ ] Реализовать `ClientRepository`.
- [ ] Реализовать `TradingAccountRepository`.
- [ ] Реализовать `InstrumentRepository`.
- [ ] Реализовать `TradeRepository`.
- [ ] Реализовать `PositionRepository`.
- [ ] Реализовать `RiskRuleRepository`.
- [ ] Реализовать `RiskAlertRepository`.
- [ ] Создать `IUnitOfWork` или использовать `DbContext` как unit of work за application boundary.
- [ ] Создать query object `GetTradesQuery`.
- [ ] Создать query object `GetPositionsQuery`.
- [ ] Создать query object `GetRiskAlertsQuery`.
- [ ] Создать query object `GetLatestQuotesQuery`.
- [ ] Создать query object `GetClientRiskMetricsQuery`.
- [ ] Добавить keyset или offset pagination для истории сделок.
- [ ] Добавить pagination для risk alerts.
- [ ] Для read-only запросов использовать `AsNoTracking` или Dapper.

## 6. Application: clients, accounts, instruments

- [ ] Реализовать `CreateClientCommand`.
- [ ] Реализовать `CreateClientHandler`.
- [ ] Реализовать `GetClientsQuery`.
- [ ] Реализовать `GetClientByIdQuery`.
- [ ] Реализовать `CreateTradingAccountCommand`.
- [ ] Реализовать `CreateTradingAccountHandler`.
- [ ] Реализовать `GetTradingAccountByIdQuery`.
- [ ] Реализовать `GetClientAccountsQuery`.
- [ ] Реализовать `CreateInstrumentCommand`.
- [ ] Реализовать `CreateInstrumentHandler`.
- [ ] Реализовать `GetInstrumentsQuery`.
- [ ] Добавить FluentValidation validators.
- [ ] Добавить unit-тесты validators для базовых ошибок.

## 7. Application: trade processing

- [ ] Реализовать `CreateTradeCommand`.
- [ ] Реализовать `CreateTradeValidator`.
- [ ] Реализовать `CreateTradeHandler`.
- [ ] В handler добавить проверку клиента.
- [ ] В handler добавить проверку торгового счета.
- [ ] В handler добавить проверку инструмента.
- [ ] В handler добавить проверку активности инструмента.
- [ ] Реализовать транзакцию: insert trade + update position.
- [ ] Реализовать расчет average price для увеличения позиции.
- [ ] Реализовать расчет net volume для Buy/Sell.
- [ ] Реализовать сценарий частичного закрытия/переворота позиции.
- [ ] Реализовать optimistic concurrency retry для позиции.
- [ ] После commit публиковать `PositionChangedEvent` в channel.
- [ ] Добавить structured logs:
  - [ ] trade accepted;
  - [ ] validation failed;
  - [ ] position updated;
  - [ ] concurrency retry.
- [ ] Добавить unit-тесты PositionCalculator.
- [ ] Добавить integration-тест "создание сделки обновляет позицию".

## 8. Event channels

- [ ] Создать модели событий:
  - [ ] `QuoteTick`;
  - [ ] `TradeAcceptedEvent`;
  - [ ] `PositionChangedEvent`;
  - [ ] `RiskEvaluationRequested`;
  - [ ] `RiskAlertRaisedEvent`.
- [ ] Создать abstraction `IEventWriter<T>`.
- [ ] Создать abstraction `IEventReader<T>`.
- [ ] Реализовать bounded channel для quote events.
- [ ] Реализовать bounded channel для risk events.
- [ ] Настроить capacity через options.
- [ ] Настроить full mode через options.
- [ ] Добавить счетчик dropped events.
- [ ] Добавить логирование переполнения channel.
- [ ] Добавить graceful completion при shutdown.

## 9. Market Data Simulator

- [ ] Реализовать `MarketDataSimulatorWorker : BackgroundService`.
- [ ] Реализовать генератор bid/ask по инструментам.
- [ ] Добавить настраиваемую частоту генерации.
- [ ] Добавить режим normal load.
- [ ] Добавить режим high load.
- [ ] Добавить режим price spike.
- [ ] Добавить start/stop управление через application service.
- [ ] Избегать `Thread.Sleep`, использовать async delay/timer с `CancellationToken`.
- [ ] Проверить корректную остановку worker'а.
- [ ] Добавить `LatestQuoteCache`.
- [ ] Реализовать `QuoteBatchWriterWorker`.
- [ ] Реализовать batch insert quotes.
- [ ] При shutdown сбрасывать остаток batch.
- [ ] Добавить integration-тест batch insert.
- [ ] Добавить логирование:
  - [ ] simulator started;
  - [ ] simulator stopped;
  - [ ] quotes generated/sec;
  - [ ] quote channel overflow.

## 10. Risk Engine

- [ ] Реализовать `RiskEventWorker : BackgroundService`.
- [ ] Обрабатывать `PositionChangedEvent`.
- [ ] Обрабатывать quote-driven risk evaluation.
- [ ] Подгружать активные risk rules.
- [ ] Подгружать account/positions/latest quotes.
- [ ] Рассчитывать `RiskMetricSnapshot`.
- [ ] Применять strategies по `RiskRuleType`.
- [ ] Создавать risk alerts через `RiskAlertFactory`.
- [ ] Реализовать alert deduplication/cooldown.
- [ ] Сохранять alert в PostgreSQL.
- [ ] Логировать alert как `Warning`.
- [ ] Логировать длительную risk evaluation.
- [ ] Добавить integration-тест "превышение лимита создает alert".
- [ ] Добавить integration-тест "disabled rule не создает alert".

## 11. API layer

- [ ] Настроить global exception handling.
- [ ] Настроить `ProblemDetails`.
- [ ] Настроить request logging.
- [ ] Настроить Swagger groups/tags.
- [ ] Реализовать `POST /api/clients`.
- [ ] Реализовать `GET /api/clients`.
- [ ] Реализовать `GET /api/clients/{id}`.
- [ ] Реализовать `POST /api/accounts`.
- [ ] Реализовать `GET /api/accounts/{id}`.
- [ ] Реализовать `GET /api/clients/{clientId}/accounts`.
- [ ] Реализовать `POST /api/instruments`.
- [ ] Реализовать `GET /api/instruments`.
- [ ] Реализовать `POST /api/trades`.
- [ ] Реализовать `GET /api/trades` с фильтрами.
- [ ] Реализовать `GET /api/clients/{clientId}/trades`.
- [ ] Реализовать `GET /api/positions`.
- [ ] Реализовать `GET /api/clients/{clientId}/positions`.
- [ ] Реализовать `GET /api/risk/clients/{clientId}`.
- [ ] Реализовать `GET /api/risk/alerts`.
- [ ] Реализовать `POST /api/risk/rules`.
- [ ] Реализовать `PUT /api/risk/rules/{id}`.
- [ ] Реализовать `POST /api/simulator/market/start`.
- [ ] Реализовать `POST /api/simulator/market/stop`.
- [ ] Реализовать `POST /api/simulator/load-test/start`.
- [ ] Реализовать `GET /api/simulator/status`.
- [ ] Добавить примеры запросов в Swagger.

## 12. Load-test scenario

- [ ] Реализовать `LoadTestWorker`.
- [ ] Добавить генерацию клиентов.
- [ ] Добавить генерацию счетов.
- [ ] Добавить генерацию сделок.
- [ ] Добавить генерацию котировок 500/sec.
- [ ] Добавить профиль 1000/sec.
- [ ] Добавить профиль 5000/sec как optional senior scenario.
- [ ] Собирать статистику:
  - [ ] trades/sec;
  - [ ] quotes/sec;
  - [ ] risk evaluations/sec;
  - [ ] average latency;
  - [ ] p95 latency;
  - [ ] dropped quotes;
  - [ ] active alerts.
- [ ] Сохранять отчет в `docs/load-tests`.
- [ ] Добавить README-раздел с результатами.

## 13. Benchmarks

- [ ] Создать benchmark для PnL calculation.
- [ ] Реализовать baseline-вариант.
- [ ] Реализовать optimized-вариант после измерений.
- [ ] Сравнить allocations.
- [ ] Сравнить mean/p95 execution time.
- [ ] Сохранить benchmark report.
- [ ] Описать выводы: что оптимизировали и почему.

## 14. Observability

- [ ] Настроить structured JSON logs.
- [ ] Добавить correlation id middleware.
- [ ] Логировать slow SQL queries.
- [ ] Логировать channel pressure.
- [ ] Логировать risk alerts.
- [ ] Добавить health checks:
  - [ ] application;
  - [ ] PostgreSQL;
  - [ ] optional Redis.
- [ ] Добавить endpoint `/health`.
- [ ] Добавить metrics endpoint optional.
- [ ] Добавить Prometheus optional.
- [ ] Добавить Grafana dashboard optional.

## 15. Docker Compose

- [ ] Создать `Dockerfile` для backend.
- [ ] Создать `docker-compose.yml`.
- [ ] Добавить сервис `pulserisk-api`.
- [ ] Добавить сервис `postgres`.
- [ ] Добавить volume для PostgreSQL.
- [ ] Добавить переменные окружения для connection string.
- [ ] Добавить `ASPNETCORE_URLS=http://+:5000`.
- [ ] Проверить доступность Swagger на `http://localhost:5000/swagger`.
- [ ] Добавить Redis optional.
- [ ] Добавить Prometheus/Grafana optional.
- [ ] Проверить `docker compose up --build` на чистом окружении.

## 16. Integration tests

- [ ] Настроить test web application factory.
- [ ] Настроить Testcontainers PostgreSQL.
- [ ] Применять migrations перед тестами.
- [ ] Добавить test data builder.
- [ ] Добавить тест "создать клиента".
- [ ] Добавить тест "создать счет".
- [ ] Добавить тест "создать инструмент".
- [ ] Добавить тест "создать сделку".
- [ ] Добавить тест "позиция обновилась после сделки".
- [ ] Добавить тест "risk alert создан при превышении exposure".
- [ ] Добавить тест "history trades фильтруется по clientId".
- [ ] Добавить тест "alerts фильтруются по severity".
- [ ] Добавить тест "concurrent trades не ломают позицию".
- [ ] Добавить тест "quote batch writer пишет batch".

## 17. Unit tests

- [ ] `PositionCalculatorTests`.
- [ ] `PnLCalculatorTests`.
- [ ] `MaxExposureRuleStrategyTests`.
- [ ] `MaxLossRuleStrategyTests`.
- [ ] `MarginLevelWarningRuleStrategyTests`.
- [ ] `PriceSpikeDetectionRuleStrategyTests`.
- [ ] `HighFrequencyTradingActivityRuleStrategyTests`.
- [ ] `CreateTradeValidatorTests`.
- [ ] `RiskAlertFactoryTests`.
- [ ] `LatestQuoteCacheTests`.
- [ ] Проверить edge cases:
  - [ ] нулевой объем;
  - [ ] отрицательная цена;
  - [ ] закрытие позиции;
  - [ ] переворот позиции;
  - [ ] отсутствие котировки;
  - [ ] отсутствие активных risk rules.

## 18. PostgreSQL performance evidence

- [ ] Подготовить тестовый набор данных.
- [ ] Выполнить неоптимизированный запрос истории сделок.
- [ ] Сохранить `EXPLAIN ANALYZE` до индекса.
- [ ] Добавить индекс.
- [ ] Сохранить `EXPLAIN ANALYZE` после индекса.
- [ ] Подготовить пример запроса последних котировок.
- [ ] Подготовить пример запроса активных алертов.
- [ ] Добавить документы в `docs/explain-analyze`.
- [ ] Описать выводы в README.

## 19. GitLab CI/CD

- [ ] Создать `.gitlab-ci.yml`.
- [ ] Добавить stage `restore`.
- [ ] Добавить stage `build`.
- [ ] Добавить stage `unit_tests`.
- [ ] Добавить stage `integration_tests`.
- [ ] Добавить stage `static_analysis`.
- [ ] Добавить stage `docker_build`.
- [ ] Публиковать test artifacts.
- [ ] Публиковать coverage artifacts.
- [ ] Кешировать NuGet packages.
- [ ] Проверить pipeline на push.
- [ ] Проверить pipeline на merge request.
- [ ] Добавить badge в README optional.

## 20. README

- [ ] Описать бизнес-контекст PulseRisk.
- [ ] Указать, что проект учебный и не предназначен для реальной торговли.
- [ ] Описать связь проекта с требованиями вакансии.
- [ ] Добавить архитектурную схему.
- [ ] Описать структуру solution.
- [ ] Описать запуск через Docker Compose.
- [ ] Описать запуск миграций.
- [ ] Описать Swagger endpoints.
- [ ] Добавить примеры curl-запросов:
  - [ ] создать клиента;
  - [ ] создать счет;
  - [ ] создать инструмент;
  - [ ] создать сделку;
  - [ ] получить позиции;
  - [ ] получить risk alerts;
  - [ ] запустить simulator.
- [ ] Описать фоновые процессы.
- [ ] Описать risk rules.
- [ ] Описать PostgreSQL schema и индексы.
- [ ] Описать запуск тестов.
- [ ] Описать CI/CD.
- [ ] Добавить раздел "Архитектурные решения".
- [ ] Добавить раздел "Performance notes".
- [ ] Добавить раздел "Что можно улучшить".

## 21. Senior-level extensions

- [ ] Реализовать SignalR hub для real-time risk metrics.
- [ ] Добавить Redis latest quote cache.
- [ ] Реализовать outbox pattern.
- [ ] Реализовать PostgreSQL advisory lock option для позиций.
- [ ] Добавить Prometheus metrics.
- [ ] Добавить Grafana dashboard.
- [ ] Реализовать CQRS read model для risk metrics.
- [ ] Добавить отдельный worker host.
- [ ] Добавить allocation optimization report.
- [ ] Добавить legacy refactoring example: baseline service -> optimized service.

## 22. Финальная приемка

- [ ] `docker compose up --build` запускает проект.
- [ ] Swagger доступен на `http://localhost:5000/swagger`.
- [ ] Можно создать клиента.
- [ ] Можно создать торговый счет.
- [ ] Можно добавить инструмент.
- [ ] Можно создать сделку.
- [ ] После сделки позиция обновляется.
- [ ] Risk Engine создает alert при нарушении лимита.
- [ ] Market Data Simulator генерирует котировки.
- [ ] Фоновые процессы корректно останавливаются.
- [ ] Данные сохраняются в PostgreSQL.
- [ ] Миграции применяются на чистую БД.
- [ ] Unit-тесты проходят.
- [ ] Integration-тесты проходят.
- [ ] GitLab CI/CD проходит.
- [ ] README позволяет запустить проект без дополнительных объяснений.
- [ ] ARCHITECTURE.md объясняет выбор паттернов и подходов.
- [ ] В документации есть разделы про performance, PostgreSQL и concurrency.

## 23. Критерии готовности к показу на интервью

- [ ] Можно за 2-3 минуты объяснить доменную модель.
- [ ] Можно показать flow создания сделки и обновления позиции.
- [ ] Можно показать risk rule strategy и unit-тесты.
- [ ] Можно показать bounded channel и backpressure.
- [ ] Можно показать индексы PostgreSQL и `EXPLAIN ANALYZE`.
- [ ] Можно показать Docker Compose запуск.
- [ ] Можно показать GitLab pipeline.
- [ ] Можно объяснить, почему проект не сделан микросервисами.
- [ ] Можно объяснить, какие части легко вынести в отдельные сервисы.
- [ ] Можно показать минимум один performance/benchmark результат.
