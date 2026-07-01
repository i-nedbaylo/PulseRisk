# Глава 16. Добавляем REST API и Swagger

## Цель главы

Сделать текущий HTTP API понятным для демонстрации и ручной проверки: сгруппировать endpoints в Swagger UI, добавить описания операций, примеры request body и типовые `ProblemDetails`-ответы.

До этой главы API уже работал, но Swagger был подключен минимально. Это полезно для smoke-проверки, но недостаточно для проекта, который должен демонстрировать инженерную зрелость: внешний контракт должен быть читаемым, проверяемым и не заставлять человека угадывать формат команд.

## Демонстрируемые компетенции

Глава показывает:

- умение держать controllers тонкими;
- осознанное описание HTTP-контракта;
- единый подход к ошибкам через `ProblemDetails`;
- аккуратное расширение Swashbuckle без захламления `Program.cs`;
- integration test, который проверяет не только код приложения, но и API-документацию.

## Что уже есть в проекте

К этому моменту реализованы:

- `GET /api/health`;
- `POST /api/clients`;
- `GET /api/clients`;
- `GET /api/clients/{id}`;
- `POST /api/accounts`;
- `GET /api/accounts/{id}`;
- `GET /api/clients/{clientId}/accounts`;
- `POST /api/instruments`;
- `GET /api/instruments`;
- `POST /api/trades`;
- `GET /api/trades/{id}`;
- `ApiExceptionMiddleware`, который возвращает `ProblemDetails`;
- `UseSerilogRequestLogging()` для structured request logs.

## Что сделаем сейчас

Добавим:

- `AddPulseRiskSwagger()` для регистрации Swagger/OpenAPI;
- `UsePulseRiskSwagger()` для настройки Swagger UI;
- `PulseRiskSwaggerOperationFilter` для summary, description, request examples и problem responses;
- `PulseRiskSwaggerTagDocumentFilter` для описания смысловых тегов;
- integration test `SwaggerEndpointTests`.

## Проектное решение

Swagger-настройка вынесена из `Program.cs` в отдельный extension:

```csharp
builder.Services.AddPulseRiskSwagger();
```

В `Development` окружении приложение включает Swagger UI:

```csharp
app.UsePulseRiskSwagger();
```

Это оставляет composition root коротким: по `Program.cs` видно, какие крупные подсистемы подключаются, но подробности OpenAPI не смешиваются с Serilog, DI и middleware pipeline.

## Паттерны GoF/GRASP

- **GRASP Controller**: API controllers по-прежнему принимают HTTP-запросы и делегируют application handlers. Они не становятся местом для OpenAPI-генерации или бизнес-логики.
- **Indirection**: `SwaggerServiceCollectionExtensions` отделяет `Program.cs` от деталей Swashbuckle. Точка подключения одна, деталей много.
- **Template Method**: Swashbuckle задает lifecycle генерации документа, а `IOperationFilter`/`IDocumentFilter` заполняют проектные детали.
- **Protected Variations**: request examples и descriptions можно менять без изменения actions, handlers и домена.
- **Information Expert**: Application commands остаются источником формы входных данных, а Swagger filter только добавляет пример их использования.

Это не overengineering: мы не вводим собственный OpenAPI framework и не расписываем отдельные атрибуты на каждую строку. Две маленькие точки расширения решают конкретную проблему: сделать API self-describing.

## Реализация шаг за шагом

### Шаг 1. Выносим Swagger в extension

Файл `src/PulseRisk.Api/OpenApi/SwaggerServiceCollectionExtensions.cs` содержит два метода:

- `AddPulseRiskSwagger()` - регистрирует Swagger generator;
- `UsePulseRiskSwagger()` - подключает Swagger middleware и UI.

В `SwaggerDoc` указываем:

- title: `PulseRisk API`;
- version: `v1`;
- description: краткое описание учебного real-time risk-management backend.

### Шаг 2. Группируем операции по тегам

`TagActionsBy` сопоставляет controllers с тегами:

- `Clients`;
- `Accounts`;
- `Instruments`;
- `Trades`;
- `Health`.

`PulseRiskSwaggerTagDocumentFilter` добавляет описание каждого тега. Это делает Swagger UI пригодным для навигации: человек видит не просто список routes, а смысловые группы.

### Шаг 3. Добавляем summary и description

`PulseRiskSwaggerOperationFilter` берет пару `ControllerName.ActionName` и назначает операции краткое описание.

Например, `Trades.Create` получает:

- summary: `Create trade`;
- description: `Accepts a trade, persists it and updates the aggregate position in one commit.`

Эта формулировка важна: endpoint не просто сохраняет запись сделки, а меняет агрегированную позицию. Swagger должен показывать это явно.

### Шаг 4. Добавляем request examples

Для write-сценариев добавлены примеры:

- `CreateClientCommand`;
- `CreateTradingAccountCommand`;
- `CreateInstrumentCommand`;
- `CreateTradeCommand`.

Пример для сделки:

```json
{
  "clientId": "11111111-1111-1111-1111-111111111111",
  "tradingAccountId": "22222222-2222-2222-2222-222222222222",
  "symbol": "EURUSD",
  "side": 1,
  "volume": 2,
  "openPrice": 1.25
}
```

Пока enum-поля оставлены числовыми, потому что текущий JSON contract использует стандартную сериализацию `System.Text.Json`. Перевод enum в строки можно сделать отдельным изменением, если мы решим менять внешний контракт осознанно.

### Шаг 5. Документируем ProblemDetails

`ApiExceptionMiddleware` уже возвращает ошибки как `application/problem+json`.

Swagger filter добавляет типовые responses:

- `400` для `POST` validation errors;
- `404` для lookup-сценариев и write-сценариев, где нужна связанная сущность;
- `409` для конфликтов состояния, например duplicate instrument или trade conflict;
- `500` для unexpected errors.

Так документация начинает совпадать с middleware, а не описывает только happy path.

### Шаг 6. Проверяем Swagger integration test

`SwaggerEndpointTests` запрашивает:

```text
/swagger/v1/swagger.json
```

Тест проверяет:

- title документа;
- наличие тегов;
- summary для `POST /api/trades`;
- request example для сделки;
- наличие `409` response.

Это маленькая, но полезная страховка: если будущий refactoring сломает Swagger generation, тест поймает проблему сразу.

## Проверка результата

Команды:

```bash
dotnet build PulseRisk.slnx
dotnet test PulseRisk.slnx --no-build
```

Результат текущей итерации:

- unit-тесты: `37/37`;
- integration tests: `6/7`;
- skipped integration tests: `1/7`.

Пропущенный тест - PostgreSQL/Testcontainers-сценарий, потому что Docker Engine на текущей машине недоступен. Swagger test выполняется без Docker и проходит.

## Что изменилось в репозитории

- `Program.cs` использует `AddPulseRiskSwagger()` и `UsePulseRiskSwagger()`.
- Добавлен `SwaggerServiceCollectionExtensions`.
- Добавлен `PulseRiskSwaggerOperationFilter`.
- Добавлен `PulseRiskSwaggerTagDocumentFilter`.
- Добавлен `SwaggerEndpointTests`.
- `DEVELOPMENT_PLAN.md` отмечает Swagger groups/tags, request logging и examples как выполненные.
- `ARCHITECTURE.md` описывает текущую Swagger-стратегию и примененные паттерны.

## Почему сделали именно так

Документация API должна быть рядом с кодом и проверяться как часть проекта. Если Swagger остается “включенным по умолчанию”, он быстро перестает быть демонстрационным инструментом: routes есть, но человек не понимает последовательность действий и формат ошибок.

Мы выбрали operation/document filters, потому что они:

- не загромождают controllers;
- работают централизованно;
- легко покрываются integration test;
- не требуют дополнительных NuGet-пакетов;
- оставляют production HTTP behavior неизменным.

## Альтернативы

- **Swagger attributes на каждом action**: просто, но быстро создает шум в controllers.
- **XML comments**: полезны для больших публичных API, но здесь пока избыточны.
- **Отдельный OpenAPI YAML**: дает полный контроль, но легко расходится с реальным кодом.
- **Генерация examples через сторонний пакет**: удобно, но для четырех команд проще поддержать маленький filter.

## Частые ошибки

- Документировать только `200/201` responses и забыть про ошибки.
- Давать пример request body, который не проходит validation.
- Менять JSON contract ради красивого Swagger без отдельного решения.
- Делать controllers ответственными за Swagger-описания, бизнес-логику и orchestration одновременно.
- Не тестировать `/swagger/v1/swagger.json`, хотя он ломается при ошибках DI или OpenAPI filters.

## Вопросы для интервью

- Почему Swagger-конфигурация вынесена из `Program.cs`?
- Чем `ProblemDetails` лучше произвольного `{ error: "..." }`?
- Почему request examples полезны даже при наличии schemas?
- Почему enum-поля пока документированы числами?
- Какие части API сейчас готовы, а какие endpoints еще сознательно не реализованы?
- Как бы вы версионировали API при появлении несовместимых изменений?

## Чек-лист главы

- [x] Swagger document получил title, version и description.
- [x] Endpoints сгруппированы по смысловым тегам.
- [x] Tags имеют описания.
- [x] Operations имеют summary/description.
- [x] Write-сценарии имеют request examples.
- [x] ProblemDetails responses добавлены в OpenAPI.
- [x] Swagger UI настроен.
- [x] Swagger JSON покрыт integration test.
- [x] Код собирается.
- [x] Проверки выполнены.
