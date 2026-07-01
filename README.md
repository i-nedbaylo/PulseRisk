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
- первые smoke-тесты и unit-тесты доменной логики;
- validators для первых write use cases;
- mapping integration tests для EF-модели;
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

Следующий технический шаг - trade processing: создание сделки, транзакционное обновление позиции и публикация события для будущего Risk Engine.
