# Глава 14. Реализуем обработку сделок

## Цель главы

Добавить первый настоящий бизнес-сценарий: API принимает сделку, Application layer проверяет входные данные и связанные сущности, доменная логика пересчитывает позицию, Infrastructure сохраняет результат в PostgreSQL через EF Core.

## Что сделано в коде

Application layer:

- `CreateTradeCommand`;
- `CreateTradeValidator`;
- `CreateTradeHandler`;
- `GetTradeByIdHandler`;
- `TradeDto`;
- `ITradeRepository`;
- `IPositionRepository`.

Infrastructure layer:

- `EfTradeRepository`;
- `EfPositionRepository`;
- DI-регистрация новых repositories.

API layer:

- `TradesController`;
- `POST /api/trades`;
- `GET /api/trades/{id}`.

Tests:

- `CreateTradeValidatorTests`;
- `CreateTradeHandlerTests`.

## Алгоритм `CreateTradeHandler`

Сценарий выполняется последовательно:

1. Проверить команду через FluentValidation.
2. Убедиться, что клиент существует.
3. Убедиться, что торговый счет существует.
4. Проверить, что счет принадлежит указанному клиенту.
5. Найти инструмент по символу.
6. Запретить сделку по неактивному инструменту.
7. Создать `Trade`.
8. Найти текущую `Position` по `(TradingAccountId, Symbol)`.
9. Если позиции нет, создать flat-position.
10. Применить сделку через `PositionCalculator`.
11. Сохранить сделку и позицию одним `SaveChangesAsync`.

В этой версии нет отдельного explicit transaction object, потому что один `SaveChangesAsync` в EF Core уже выполняет изменения атомарно. Явная транзакция понадобится, когда появятся несколько сохранений, outbox-запись, retry policy или дополнительные SQL-команды в одном use case.

## Паттерны GoF/GRASP

- **GRASP Controller**: `TradesController` принимает HTTP-запрос и передает его в `CreateTradeHandler`. Контроллер не знает, как пересчитывается позиция.
- **Command style**: `CreateTradeCommand` является явным input model для use case. Это упрощает validation, тестирование и будущий logging.
- **GRASP Information Expert**: формула обновления позиции находится в `PositionCalculator`, а не в API или EF repository. Именно этот компонент знает, как меняются `NetVolume` и `AveragePrice`.
- **GRASP Pure Fabrication**: `CreateTradeHandler` не является доменной сущностью, но нужен как orchestration object: он связывает validation, repositories, доменный расчет и commit.
- **Repository + Adapter**: Application layer зависит от `ITradeRepository` и `IPositionRepository`, а EF Core подключается в Infrastructure через `EfTradeRepository` и `EfPositionRepository`.
- **Unit of Work**: `IUnitOfWork` фиксирует границу commit. Сделка и позиция сохраняются вместе, поэтому не возникает состояния "сделка есть, позиция не обновлена".
- **Protected Variations**: будущая замена EF-запросов на Dapper или добавление concurrency strategy не изменит `TradesController`.

Это не overengineering: каждый паттерн стоит на границе, где уже есть реальная причина для изменения - HTTP, use case, доменная формула, persistence или commit. Внешний mediator, outbox и event channel пока не добавлены, потому что без Risk Engine они бы усложнили код раньше времени.

## Что пока сознательно не сделано

- optimistic concurrency retry для конкурентных сделок по одной позиции;
- публикация `PositionChangedEvent` после commit;
- structured logs для `trade accepted`, `position updated`, `concurrency retry`;
- Testcontainers-сценарий "создание сделки обновляет позицию" на настоящей PostgreSQL.

Эти задачи остаются следующими шагами. Так книга показывает реалистичный путь разработки: сначала корректный use case и unit-тесты, затем укрепление конкурентности и integration coverage.

## Проверка результата

Команды:

```bash
dotnet build PulseRisk.slnx
dotnet test PulseRisk.slnx --no-build
```

Результат текущей итерации:

- unit-тесты: `30/30`;
- integration tests: `4/4`.

## Чек-лист главы

- [x] `CreateTradeCommand` добавлен.
- [x] `CreateTradeValidator` добавлен.
- [x] `CreateTradeHandler` добавлен.
- [x] Проверки клиента, счета и инструмента добавлены.
- [x] Неактивный инструмент запрещен для новой сделки.
- [x] Новая позиция создается при первой сделке.
- [x] Существующая позиция обновляется через `PositionCalculator`.
- [x] Сделка и позиция сохраняются одним commit.
- [x] `POST /api/trades` добавлен.
- [x] Unit-тесты handler/validator проходят.
