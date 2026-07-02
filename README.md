# PulseRisk

**PulseRisk** - учебно-демонстрационный backend-проект на .NET 10, спроектированный как демонстрация инженерных компетенций для real-time fintech/risk-management backend.

Проект имитирует ядро fintech-сервиса риск-менеджмента для внебиржевого рынка: клиенты, торговые счета, инструменты, сделки, позиции, котировки, риск-правила и алерты. Цель проекта - показать не CRUD-приложение, а backend с доменной моделью, PostgreSQL, конкурентной обработкой событий, тестами, Docker и CI/CD.

## Текущий статус

Сейчас создан стартовый репозиторий:

- локальный git-репозиторий;
- .NET 10 solution `PulseRisk.slnx`;
- проекты `Api`, `Application`, `Domain`, `Infrastructure`, `BackgroundWorkers`;
- проекты `UnitTests`, `IntegrationTests`, `Benchmarks`;
- базовые зависимости для EF Core/PostgreSQL, Dapper, FluentValidation, Serilog, Testcontainers и BenchmarkDotNet;
- health endpoint `GET /api/health`;
- доменный слой: enums, value objects, entities, `PositionCalculator`, `PnLCalculator`;
- инфраструктурный слой: `PulseRiskDbContext`, EF Core mappings, PostgreSQL migration, seed instruments/risk rules;
- application/API слой для clients, accounts и instruments;
- trade processing: `POST /api/trades`, создание сделки и обновление позиции за один commit;
- optimistic concurrency retry для обновления позиции через PostgreSQL `xmin`;
- configurable in-process bounded channels для quote/risk events;
- backpressure diagnostics: depth, written/read/dropped counters и graceful completion каналов;
- background worker, который превращает `PositionChangedEvent` в `RiskEvaluationRequested`;
- Risk Engine: `RiskEvaluationRequestedWorker`, active rules/latest quote ports и расчет `RiskMetricSnapshot`;
- risk rule strategies для exposure, loss и margin level с resolver-ом и unit-тестами;
- создание `RiskAlert` через factory, active-alert deduplication, сохранение в PostgreSQL и публикация `RiskAlertRaisedEvent`;
- отключаемый Market Data Simulator, который генерирует `QuoteTick` по настраиваемой частоте и пишет их в quote channel;
- `QuoteBatchWriterWorker`, который читает `QuoteTick`, пишет котировки batch'ами в PostgreSQL, сбрасывает остаток batch при shutdown и запускает quote-driven risk evaluation для открытых позиций;
- Swagger/OpenAPI с группами endpoints, описаниями операций, request examples и ProblemDetails-ответами;
- первые smoke-тесты и unit-тесты доменной логики;
- validators для первых write use cases;
- unit-тесты orchestration для создания сделки;
- расширенное unit-покрытие Risk Engine edge cases: отсутствие котировки, отсутствие active rules, disabled rules, unsupported rule strategy и threshold boundaries;
- mapping integration tests для EF-модели;
- Testcontainers-сценарий `client -> account -> trade -> position` на настоящей PostgreSQL, который автоматически пропускается, если Docker Engine недоступен;
- каркас книги в `book/`.

## Быстрый старт

Собрать solution:

```bash
dotnet build PulseRisk.slnx
```

Запустить тесты:

```bash
dotnet test PulseRisk.slnx
```

Восстановить локальные .NET tools:

```bash
dotnet tool restore
```

Создать или обновить локальную PostgreSQL-схему после запуска БД:

```bash
dotnet ef database update --project src/PulseRisk.Infrastructure --startup-project src/PulseRisk.Api
```

Запустить API локально:

```bash
dotnet run --project src/PulseRisk.Api
```

Проверить health endpoint:

```bash
curl http://localhost:5000/api/health
```

## Документы

- `PROJECT_REQUIREMENTS.md` - нейтральный перечень компетенций и технических ожиданий, которые должен продемонстрировать проект.
- `ARCHITECTURE.md` - архитектурное описание проекта.
- `DEVELOPMENT_PLAN.md` - детальный чек-лист разработки.
- `BOOK_PLAN.md` - подробный план книги.
- `book/` - будущая книга, которая пишется параллельно с кодом.

## Следующий этап

Следующий технический шаг - укрепить integration tests: сквозной сценарий превышения лимита должен создавать risk alert в PostgreSQL.
