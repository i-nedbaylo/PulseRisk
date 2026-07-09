# PulseRisk: детальный план разработки

План ориентирован на учебно-демонстрационный проект, который должен показать инженерные компетенции в разработке real-time fintech/risk-management backend. Главный акцент - конкурентная обработка событий, PostgreSQL, производительность, тестируемость и понятная архитектура без overengineering.

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
  - [x] `docs/load-tests`;
  - [x] `docs/benchmarks`.
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
  - [x] `MarketDataOptions`;
  - [ ] `RiskEngineOptions`;
  - [x] `EventChannelOptions`;
  - [x] `QuoteBatchOptions`;
  - [ ] `LoadTestOptions`.
- [ ] Настроить validation options на старте приложения.

## 2. Domain layer

- [x] Создать enums:
  - [x] `ClientStatus`;
  - [x] `TradeSide`;
  - [x] `RiskRuleType`;
  - [x] `RiskAlertType`;
  - [x] `RiskSeverity`;
  - [x] `CurrencyCode`.
- [x] Создать value objects:
  - [x] `Symbol`;
  - [x] `Money`;
  - [x] `Price`;
  - [x] `Volume`;
  - [x] `Percentage`.
- [x] Реализовать entity `Client`.
- [x] Реализовать entity `TradingAccount`.
- [x] Реализовать entity `Instrument`.
- [x] Реализовать entity `Trade`.
- [x] Реализовать entity `Position`.
- [x] Реализовать entity `Quote`.
- [x] Реализовать entity `RiskRule`.
- [x] Реализовать entity `RiskAlert`.
- [ ] Добавить доменные инварианты:
  - [x] volume > 0;
  - [x] price > 0;
  - [x] bid <= ask;
  - [x] leverage > 0;
  - [x] inactive instrument нельзя использовать для новой сделки;
  - [x] disabled risk rule не участвует в проверке.
- [x] Реализовать доменный сервис `PositionCalculator`.
- [x] Реализовать доменный сервис `PnLCalculator`.
- [x] Реализовать модели risk evaluation:
  - [x] `RiskEvaluationContext`;
  - [x] `RiskMetricSnapshot`;
  - [x] `RiskRuleEvaluationResult`.

## 3. Risk rules

- [x] Создать интерфейс `IRiskRuleStrategy`.
- [x] Реализовать resolver `RiskRuleStrategyResolver`.
- [x] Реализовать `MaxExposureRuleStrategy`.
- [x] Реализовать `MaxLossRuleStrategy`.
- [x] Реализовать `MarginLevelWarningRuleStrategy`.
- [ ] Реализовать `PriceSpikeDetectionRuleStrategy`.
- [ ] Реализовать `HighFrequencyTradingActivityRuleStrategy`.
- [x] Создать `RiskAlertFactory`.
- [x] Добавить cooldown/deduplication модель для алертов.
- [ ] Покрыть каждое правило unit-тестами.
- [x] Проверить сценарий: rule disabled не создает alert.
- [x] Проверить сценарий: несколько rules могут создать несколько alert'ов в одной evaluation.

## 4. Infrastructure: PostgreSQL и EF Core

- [x] Создать `PulseRiskDbContext`.
- [x] Настроить mappings через `IEntityTypeConfiguration<T>`.
- [x] Настроить snake_case naming convention.
- [x] Настроить precision для денежных и ценовых полей.
- [x] Настроить enum conversion.
- [x] Настроить `xmin`/concurrency token для `Position`, если выбран optimistic concurrency.
- [x] Создать initial migration.
- [x] Создать таблицу `clients`.
- [x] Создать таблицу `trading_accounts`.
- [x] Создать таблицу `instruments`.
- [x] Создать таблицу `trades`.
- [x] Создать таблицу `positions`.
- [x] Создать таблицу `quotes`.
- [x] Создать таблицу `risk_rules`.
- [x] Создать таблицу `risk_alerts`.
- [x] Добавить обязательные индексы:
  - [x] `trades(client_id, created_at DESC)`;
  - [x] `trades(symbol, created_at DESC)`;
  - [x] `positions(client_id, symbol)`;
  - [x] unique `positions(trading_account_id, symbol)`;
  - [x] `quotes(symbol, timestamp DESC)`;
  - [x] `risk_alerts(client_id, created_at DESC)`;
  - [x] `risk_alerts(severity, created_at DESC)`.
- [x] Добавить partial index для активных алертов.
- [x] Добавить seed базовых инструментов:
  - [x] `EURUSD`;
  - [x] `GBPUSD`;
  - [x] `XAUUSD`.
- [x] Добавить seed базовых risk rules.

## 5. Infrastructure: repositories и query objects

- [x] Создать интерфейсы repositories в Application layer.
- [x] Реализовать `ClientRepository`.
- [x] Реализовать `TradingAccountRepository`.
- [x] Реализовать `InstrumentRepository`.
- [x] Реализовать `TradeRepository`.
- [x] Реализовать `PositionRepository`.
- [x] Реализовать `RiskRuleRepository`.
- [x] Реализовать `RiskAlertRepository`.
- [x] Создать `IUnitOfWork` или использовать `DbContext` как unit of work за application boundary.
- [ ] Создать query object `GetTradesQuery`.
- [ ] Создать query object `GetPositionsQuery`.
- [ ] Создать query object `GetRiskAlertsQuery`.
- [ ] Создать query object `GetLatestQuotesQuery`.
- [ ] Создать query object `GetClientRiskMetricsQuery`.
- [ ] Добавить keyset или offset pagination для истории сделок.
- [ ] Добавить pagination для risk alerts.
- [ ] Для read-only запросов использовать `AsNoTracking` или Dapper.

## 6. Application: clients, accounts, instruments

- [x] Реализовать `CreateClientCommand`.
- [x] Реализовать `CreateClientHandler`.
- [x] Реализовать `GetClientsQuery`.
- [x] Реализовать `GetClientByIdQuery`.
- [x] Реализовать `CreateTradingAccountCommand`.
- [x] Реализовать `CreateTradingAccountHandler`.
- [x] Реализовать `GetTradingAccountByIdQuery`.
- [x] Реализовать `GetClientAccountsQuery`.
- [x] Реализовать `CreateInstrumentCommand`.
- [x] Реализовать `CreateInstrumentHandler`.
- [x] Реализовать `GetInstrumentsQuery`.
- [x] Добавить FluentValidation validators.
- [x] Добавить unit-тесты validators для базовых ошибок.

## 7. Application: trade processing

- [x] Реализовать `CreateTradeCommand`.
- [x] Реализовать `CreateTradeValidator`.
- [x] Реализовать `CreateTradeHandler`.
- [x] В handler добавить проверку клиента.
- [x] В handler добавить проверку торгового счета.
- [x] В handler добавить проверку инструмента.
- [x] В handler добавить проверку активности инструмента.
- [x] Реализовать транзакцию: insert trade + update position.
- [x] Реализовать расчет average price для увеличения позиции.
- [x] Реализовать расчет net volume для Buy/Sell.
- [x] Реализовать сценарий частичного закрытия/переворота позиции.
- [x] Реализовать optimistic concurrency retry для позиции.
- [x] После commit публиковать `PositionChangedEvent` в channel.
- [x] Добавить structured logs:
  - [x] trade accepted;
  - [x] validation failed;
  - [x] position updated;
  - [x] concurrency retry.
- [x] Добавить unit-тесты PositionCalculator.
- [x] Добавить integration-тест "создание сделки обновляет позицию".

## 8. Event channels

- [x] Создать модели событий:
  - [x] `QuoteTick`;
  - [x] `TradeAcceptedEvent`;
  - [x] `PositionChangedEvent`;
  - [x] `RiskEvaluationRequested`;
  - [x] `RiskAlertRaisedEvent`.
- [x] Создать abstraction `IEventWriter<T>`.
- [x] Создать abstraction `IEventReader<T>`.
- [x] Реализовать bounded channel для quote events.
- [x] Реализовать bounded channel для risk events.
- [x] Настроить capacity через options.
- [x] Настроить full mode через options.
- [x] Добавить счетчик dropped events.
- [x] Добавить логирование переполнения channel.
- [x] Добавить graceful completion при shutdown.

## 9. Market Data Simulator

- [x] Реализовать `MarketDataSimulatorWorker : BackgroundService`.
- [x] Реализовать генератор bid/ask по инструментам.
- [x] Добавить настраиваемую частоту генерации.
- [x] Добавить режим normal load.
- [ ] Добавить режим high load.
- [ ] Добавить режим price spike.
- [ ] Добавить start/stop управление через application service.
- [x] Избегать `Thread.Sleep`, использовать async delay/timer с `CancellationToken`.
- [ ] Проверить корректную остановку worker'а.
- [ ] Добавить `LatestQuoteCache`.
- [x] Реализовать `QuoteBatchWriterWorker`.
- [x] Реализовать batch insert quotes.
- [x] Публиковать quote-driven risk requests после сохранения batch-а котировок.
- [x] При shutdown сбрасывать остаток batch.
- [x] Добавить integration-тест batch insert.
- [ ] Добавить логирование:
  - [x] simulator started;
  - [x] simulator stopped;
  - [x] quotes generated/sec;
  - [ ] quote channel overflow.

## 10. Risk Engine

- [x] Реализовать `RiskEventWorker : BackgroundService`.
- [x] Обрабатывать `PositionChangedEvent`.
- [x] Обрабатывать quote-driven risk evaluation.
- [x] Подгружать активные risk rules.
- [x] Подгружать account/positions/latest quotes.
- [x] Рассчитывать `RiskMetricSnapshot`.
- [x] Применять strategies по `RiskRuleType`.
- [x] Создавать risk alerts через `RiskAlertFactory`.
- [x] Реализовать alert deduplication/cooldown.
- [x] Сохранять alert в PostgreSQL.
- [x] Логировать alert как `Warning`.
- [ ] Логировать длительную risk evaluation.
- [ ] Добавить integration-тест "превышение лимита создает alert".
- [ ] Добавить integration-тест "disabled rule не создает alert".

## 11. API layer

- [x] Настроить global exception handling.
- [x] Настроить `ProblemDetails`.
- [x] Настроить request logging.
- [x] Настроить Swagger groups/tags.
- [x] Реализовать `POST /api/clients`.
- [x] Реализовать `GET /api/clients`.
- [x] Реализовать `GET /api/clients/{id}`.
- [x] Реализовать `POST /api/accounts`.
- [x] Реализовать `GET /api/accounts/{id}`.
- [x] Реализовать `GET /api/clients/{clientId}/accounts`.
- [x] Реализовать `POST /api/instruments`.
- [x] Реализовать `GET /api/instruments`.
- [x] Реализовать `POST /api/trades`.
- [x] Реализовать `GET /api/trades/{id}`.
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
- [x] Добавить примеры запросов в Swagger.

## 12. Load-test scenario

- [x] Реализовать `LoadTestWorker`.
- [x] Добавить генерацию клиентов.
- [x] Добавить генерацию счетов.
- [x] Добавить генерацию сделок.
- [x] Добавить генерацию котировок 500/sec.
- [x] Добавить профиль 1000/sec.
- [x] Добавить профиль 5000/sec как optional senior scenario.
- [x] Собирать статистику:
  - [x] trades/sec;
  - [x] quotes/sec;
  - [x] risk evaluations/sec;
  - [x] average latency;
  - [x] p95 latency;
  - [x] dropped quotes;
  - [x] active alerts.
- [x] Сохранять отчет в `docs/load-tests`.
- [x] Добавить README-раздел с результатами.

## 13. Benchmarks

- [x] Создать benchmark для PnL calculation.
- [x] Реализовать baseline-вариант.
- [x] Реализовать optimized-вариант после измерений.
- [x] Сравнить allocations.
- [x] Сравнить mean/p95 execution time.
- [x] Сохранить benchmark report.
- [x] Описать выводы: что оптимизировали и почему.

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

- [x] Создать `Dockerfile` для backend.
- [x] Создать `docker-compose.yml`.
- [x] Добавить сервис `pulserisk-api`.
- [x] Добавить сервис `postgres`.
- [x] Добавить volume для PostgreSQL.
- [x] Добавить переменные окружения для connection string.
- [x] Добавить `ASPNETCORE_URLS=http://+:5000`.
- [x] Проверить доступность Swagger на `http://localhost:5000/swagger`.
- [ ] Добавить Redis optional.
- [ ] Добавить Prometheus/Grafana optional.
- [x] Проверить `docker compose up --build` на чистом окружении.

## 16. Integration tests

- [x] Настроить test web application factory.
- [x] Настроить Testcontainers PostgreSQL.
- [x] Применять migrations перед тестами.
- [x] Добавить test data builder.
- [x] Добавить тест "создать клиента".
- [x] Добавить тест "создать счет".
- [ ] Добавить тест "создать инструмент".
- [x] Добавить тест "создать сделку".
- [x] Добавить тест "позиция обновилась после сделки".
- [x] Добавить тест "risk alert создан при превышении exposure".
- [ ] Добавить тест "history trades фильтруется по clientId".
- [ ] Добавить тест "alerts фильтруются по severity".
- [ ] Добавить тест "concurrent trades не ломают позицию".
- [x] Добавить тест "quote batch writer пишет batch".

## 17. Unit tests

- [x] `PositionCalculatorTests`.
- [x] `PnLCalculatorTests`.
- [x] `MaxExposureRuleStrategyTests`.
- [x] `MaxLossRuleStrategyTests`.
- [x] `MarginLevelWarningRuleStrategyTests`.
- [ ] `PriceSpikeDetectionRuleStrategyTests`.
- [ ] `HighFrequencyTradingActivityRuleStrategyTests`.
- [x] `CreateTradeValidatorTests`.
- [x] `RiskAlertFactoryTests`.
- [x] `QuoteRiskEvaluationDispatcherTests`.
- [ ] `LatestQuoteCacheTests`.
- [x] Проверить edge cases:
  - [x] нулевой объем;
  - [x] отрицательная цена;
  - [x] закрытие позиции;
  - [x] переворот позиции;
  - [x] отсутствие котировки;
  - [x] отсутствие активных risk rules.

## 18. PostgreSQL performance evidence

- [x] Подготовить тестовый набор данных.
- [x] Выполнить baseline hot-path запросы.
- [x] Сохранить `EXPLAIN ANALYZE` до индекса для quote-driven open positions lookup.
- [x] Добавить индекс.
- [x] Сохранить `EXPLAIN ANALYZE` после индекса.
- [x] Подготовить пример запроса последних котировок.
- [x] Подготовить пример запроса активных алертов.
- [x] Добавить документы в `docs/explain-analyze`.
- [x] Описать выводы в README.

## 19. GitHub Actions CI/CD

- [x] Создать `.github/workflows/ci.yml`.
- [x] Добавить job `restore` или общий restore step с NuGet cache.
- [x] Добавить job `build`.
- [x] Добавить job `unit_tests`.
- [x] Добавить job `integration_tests`.
- [x] Добавить job `static_analysis`.
- [x] Добавить job `docker_build`.
- [x] Публиковать test artifacts.
- [x] Публиковать coverage artifacts.
- [x] Кешировать NuGet packages.
- [x] Проверить workflow на push.
- [x] Проверить workflow на pull request.
- [x] Подключить required status checks в GitHub branch protection для `main`.
- [ ] Добавить badge в README optional.

## 20. GitLab CI/CD

- [x] Создать `.gitlab-ci.yml`.
- [x] Добавить stage `restore`.
- [x] Добавить stage `build`.
- [x] Добавить stage `unit_tests`.
- [x] Добавить stage `integration_tests`.
- [x] Добавить stage `static_analysis`.
- [x] Добавить stage `docker_build`.
- [x] Публиковать test artifacts.
- [x] Публиковать coverage artifacts.
- [x] Кешировать NuGet packages.
- [ ] Проверить pipeline на push.
- [ ] Проверить pipeline на merge request.
- [ ] Добавить badge в README optional.

## 21. README

- [ ] Описать бизнес-контекст PulseRisk.
- [ ] Указать, что проект учебный и не предназначен для реальной торговли.
- [ ] Описать, какие инженерные компетенции демонстрирует проект.
- [ ] Добавить архитектурную схему.
- [ ] Описать структуру solution.
- [x] Описать запуск через Docker Compose.
- [x] Описать запуск миграций.
- [x] Описать Swagger endpoints.
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
- [x] Описать CI/CD.
- [ ] Добавить раздел "Архитектурные решения".
- [ ] Добавить раздел "Performance notes".
- [ ] Добавить раздел "Что можно улучшить".

## 22. Senior-level extensions

- [ ] Реализовать SignalR hub для real-time risk metrics.
- [ ] Добавить Redis latest quote cache.
- [ ] Реализовать outbox pattern.
- [ ] Реализовать PostgreSQL advisory lock option для позиций.
- [ ] Добавить Prometheus metrics.
- [ ] Добавить Grafana dashboard.
- [ ] Реализовать CQRS read model для risk metrics.
- [ ] Добавить отдельный worker host.
- [x] Добавить allocation optimization report.
- [ ] Добавить legacy refactoring example: baseline service -> optimized service.

## 23. Финальная приемка

- [x] `docker compose up --build` запускает проект.
- [x] Swagger доступен на `http://localhost:5000/swagger`.
- [ ] Можно создать клиента.
- [ ] Можно создать торговый счет.
- [ ] Можно добавить инструмент.
- [ ] Можно создать сделку.
- [ ] После сделки позиция обновляется.
- [ ] Risk Engine создает alert при нарушении лимита.
- [ ] Market Data Simulator генерирует котировки.
- [ ] Фоновые процессы корректно останавливаются.
- [x] Данные сохраняются в PostgreSQL.
- [x] Миграции применяются на чистую БД.
- [ ] Unit-тесты проходят.
- [ ] Integration-тесты проходят.
- [ ] GitLab CI/CD проходит.
- [ ] README позволяет запустить проект без дополнительных объяснений.
- [ ] ARCHITECTURE.md объясняет выбор паттернов и подходов.
- [ ] В документации есть разделы про performance, PostgreSQL и concurrency.

## 24. Критерии готовности к показу на интервью

- [ ] Можно за 2-3 минуты объяснить доменную модель.
- [ ] Можно показать flow создания сделки и обновления позиции.
- [ ] Можно показать risk rule strategy и unit-тесты.
- [ ] Можно показать bounded channel и backpressure.
- [ ] Можно показать индексы PostgreSQL и `EXPLAIN ANALYZE`.
- [x] Можно показать Docker Compose запуск.
- [x] Можно показать GitLab pipeline.
- [ ] Можно объяснить, почему проект не сделан микросервисами.
- [ ] Можно объяснить, какие части легко вынести в отдельные сервисы.
- [x] Можно показать минимум один performance/benchmark результат.
