# Глава 11. Подключаем PostgreSQL и EF Core

## Цель главы

Добавить persistence layer: `PulseRiskDbContext`, PostgreSQL provider, EF Core mappings и design-time tooling.

## Демонстрируемые компетенции

Глава показывает, что инфраструктура подключается снаружи домена. Домен не знает о PostgreSQL, EF Core и миграциях, а Infrastructure выступает адаптером между доменной моделью и базой данных.

## Что сделано в коде

- Добавлен `PulseRiskDbContext`.
- Добавлена регистрация `AddPulseRiskInfrastructure`.
- Добавлен connection string `PulseRisk`.
- Добавлен design-time factory `PulseRiskDesignTimeDbContextFactory`.
- Добавлен локальный tool manifest с `dotnet-ef 10.0.9`.
- ASP.NET OpenAPI template заменен на `Swashbuckle.AspNetCore`, чтобы избежать уязвимой transitive-зависимости и оставить Swagger UI.

## Паттерны GoF/GRASP

- **Adapter**: Infrastructure адаптирует EF Core/PostgreSQL к application/domain модели.
- **Unit of Work**: `DbContext` станет транзакционной границей для сценария `Trade + Position`.
- **Low Coupling**: домен не зависит от persistence API.

Это не overengineering: без отдельного Infrastructure layer доменные классы быстро начали бы зависеть от EF Core annotations, connection strings и SQL-specific деталей.

## Проверка результата

Команды:

```bash
dotnet build PulseRisk.slnx
dotnet test PulseRisk.slnx
```

Результат текущей итерации:

- build без предупреждений;
- unit-тесты: `14/14`;
- integration tests: `4/4`.

## Чек-лист главы

- [x] DbContext добавлен.
- [x] PostgreSQL provider подключен.
- [x] Infrastructure регистрируется через DI.
- [x] Design-time factory добавлена.
- [x] Локальный `dotnet-ef` зафиксирован.

