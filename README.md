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
- первые smoke-тесты;
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

Следующий технический шаг - реализация доменной модели: value objects, enums, entities и первые unit-тесты для расчета позиции.
