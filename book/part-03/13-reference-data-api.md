# Глава 13. Реализуем clients, accounts и instruments

## Цель главы

Добавить первые полноценные write/read use cases и REST endpoints: клиенты, торговые счета и инструменты.

## Демонстрируемые компетенции

Глава показывает переход от домена и persistence к application orchestration. API остается тонким, бизнес-сценарии живут в handlers, а SQL-детали скрыты за repository interfaces.

## Что сделано в коде

Application layer:

- `CreateClientCommand`;
- `CreateClientHandler`;
- `GetClientsHandler`;
- `GetClientByIdHandler`;
- `CreateTradingAccountCommand`;
- `CreateTradingAccountHandler`;
- `GetTradingAccountByIdHandler`;
- `GetClientAccountsHandler`;
- `CreateInstrumentCommand`;
- `CreateInstrumentHandler`;
- `GetInstrumentsHandler`;
- validators для всех create-команд;
- DTO для clients, accounts, instruments;
- repository ports;
- `IUnitOfWork`;
- `EntityNotFoundException`;
- `ConflictException`.

Infrastructure layer:

- `EfClientRepository`;
- `EfTradingAccountRepository`;
- `EfInstrumentRepository`;
- `EfUnitOfWork`;
- регистрация repositories в DI.

API layer:

- `ClientsController`;
- `AccountsController`;
- `InstrumentsController`;
- `ApiExceptionMiddleware` для `ProblemDetails`.

## Паттерны GoF/GRASP

- **GRASP Controller**: ASP.NET controllers принимают HTTP-запрос и передают его в application handler, не выполняя бизнес-логику.
- **Command style**: create-сценарии получают явные input records: `CreateClientCommand`, `CreateTradingAccountCommand`, `CreateInstrumentCommand`.
- **Repository**: Infrastructure скрывает EF Core details от Application layer.
- **Unit of Work**: `EfUnitOfWork` централизует `SaveChangesAsync`.
- **Adapter**: EF repositories адаптируют PostgreSQL/EF Core к application ports.

Это не overengineering: каждый паттерн закрывает реальную границу - HTTP, orchestration, persistence или transaction commit. Внешний mediator пока не вводится, потому что прямые handlers проще и достаточны.

## Проверка результата

Команды:

```bash
dotnet build PulseRisk.slnx
dotnet test PulseRisk.slnx
```

Результат текущей итерации:

- unit-тесты: `22/22`;
- integration tests: `4/4`.

## Чек-лист главы

- [x] Create/get handlers для clients добавлены.
- [x] Create/get handlers для accounts добавлены.
- [x] Create/get handlers для instruments добавлены.
- [x] Validators добавлены.
- [x] Repositories добавлены.
- [x] Controllers добавлены.
- [x] API errors возвращаются как `ProblemDetails`.

