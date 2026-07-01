# Глава 12. Проектируем схему БД и миграции

## Цель главы

Создать нормализованную PostgreSQL-схему через EF Core migration: таблицы, индексы, foreign keys и seed data.

## Демонстрируемые компетенции

Глава показывает осознанную работу с PostgreSQL: schema-first thinking через EF mappings, индексы под реальные запросы, транзакционные будущие сценарии и подготовку к `EXPLAIN ANALYZE`.

## Что сделано в коде

Созданы mappings через `IEntityTypeConfiguration<T>` для:

- `Client`;
- `TradingAccount`;
- `Instrument`;
- `Trade`;
- `Position`;
- `Quote`;
- `RiskRule`;
- `RiskAlert`.

Initial migration создает:

- таблицы `clients`, `trading_accounts`, `instruments`, `trades`, `positions`, `quotes`, `risk_rules`, `risk_alerts`;
- foreign keys между клиентами, счетами, инструментами, сделками, позициями, котировками и алертами;
- индексы для истории сделок, позиций, последних котировок и risk alerts;
- partial index для активных risk alerts;
- seed базовых инструментов `EURUSD`, `GBPUSD`, `XAUUSD`;
- seed базовых risk rules.

## Паттерны GoF/GRASP

- **Query Object, подготовка**: индексы проектируются под будущие read scenarios, а не просто под структуру сущностей.
- **Protected Variations**: value object conversions изолируют доменные типы от формата хранения.
- **Unit of Work**: схема готовит атомарное сохранение сделки и позиции в одной транзакции.

GoF-паттерны здесь не вводятся: задача главы - persistence mapping, а не алгоритмическая вариативность.

## Проверка результата

Миграция создана командой:

```bash
dotnet dotnet-ef migrations add InitialCreate --project src/PulseRisk.Infrastructure --startup-project src/PulseRisk.Api --output-dir Persistence/Migrations
```

Mapping tests проверяют:

- ожидаемые имена таблиц;
- индексы для `trades`;
- partial index активных risk alerts.

## Чек-лист главы

- [x] Initial migration создана.
- [x] Таблицы созданы.
- [x] Foreign keys добавлены.
- [x] Индексы добавлены.
- [x] Seed data добавлены.
- [x] EF-модель проверена тестами.

