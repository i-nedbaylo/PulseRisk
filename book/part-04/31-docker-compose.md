# Глава 31. Упаковываем проект в Docker Compose

## Цель главы

Сделать первый Docker Compose контур для PulseRisk: API и PostgreSQL должны подниматься одной командой, а приложение должно получать connection string и runtime-настройки через environment variables.

До этой главы проект уже умел:

- хранить данные в PostgreSQL;
- применять EF Core migrations вручную;
- запускать API локально через `dotnet run`;
- проверять бизнес-сценарии через Testcontainers;
- измерять SQL, load-test и domain benchmark results.

Теперь нужен следующий практический шаг: воспроизводимое локальное окружение.

## Демонстрируемые компетенции

Глава показывает:

- multi-stage Dockerfile для .NET 10 API;
- Docker Compose service graph;
- PostgreSQL named volume;
- healthcheck для БД;
- передачу connection string через environment variables;
- startup migrations как явно включаемую опцию;
- Swagger/health smoke endpoints;
- честное описание внешней registry/network нестабильности как эксплуатационного наблюдения, а не дефекта кода.

## Архитектурное решение

Compose содержит два сервиса:

```text
pulserisk-api
postgres
```

`pulserisk-api` собирается из `Dockerfile`.

`postgres` использует image:

```text
postgres:17.4
```

PostgreSQL не публикуется на host port. API обращается к нему внутри compose network по имени сервиса:

```text
Host=postgres;Port=5432
```

Это уменьшает риск конфликта с локальной PostgreSQL на `localhost:5432`.

## Dockerfile

Dockerfile сделан multi-stage:

```text
sdk:10.0 -> restore/publish
aspnet:10.0 -> runtime
```

Сначала копируются только project files:

```text
PulseRisk.Domain.csproj
PulseRisk.Application.csproj
PulseRisk.Infrastructure.csproj
PulseRisk.BackgroundWorkers.csproj
PulseRisk.Api.csproj
```

Потом выполняется restore, затем копируется `src/` и выполняется publish.

Такой порядок нужен для Docker layer cache: изменение `.cs` файлов не должно каждый раз инвалидировать NuGet restore layer.

## .dockerignore

`.dockerignore` исключает:

- `.git`;
- IDE state;
- `bin/`;
- `obj/`;
- `TestResults/`;
- benchmark artifacts;
- coverage;
- локальные `.env`;
- `book/` и `docs/`.

Документация важна для репозитория, но API image она не нужна.

## Docker Compose

Основная команда:

```bash
docker compose up --build
```

API получает настройки:

```yaml
ASPNETCORE_ENVIRONMENT: Development
ASPNETCORE_URLS: http://+:5000
ConnectionStrings__PulseRisk: Host=postgres;Port=5432;Database=pulserisk;Username=pulserisk;Password=pulserisk;GSS Encryption Mode=Disable
Database__ApplyMigrationsOnStartup: "true"
MarketData__Enabled: "false"
LoadTest__Enabled: "false"
```

Почему `Development`?

Эта compose-конфигурация пока демонстрационная. Она должна показывать Swagger UI по адресу:

```text
http://localhost:5000/swagger
```

Для production-like image позже можно добавить отдельный compose profile или override-файл.

Почему `GSS Encryption Mode=Disable`?

В demo-контуре PostgreSQL использует обычную username/password-аутентификацию. Kerberos/GSS здесь не нужен. Npgsql 10 по умолчанию может пробовать GSS-API в Linux-container окружении и писать безвредный, но шумный log message про отсутствие `libgssapi_krb5.so.2`. Явное отключение GSS probe делает startup logs чище и не добавляет отдельный security-механизм туда, где он не используется.

## Startup migrations

В API добавлен extension:

```csharp
await app.ApplyPulseRiskDatabaseMigrationsAsync();
```

Он применяет миграции только если включен флаг:

```text
Database:ApplyMigrationsOnStartup=true
```

Это важно.

Автоматические миграции на старте удобны для demo/local Compose, но в production их часто выносят в отдельный deployment step. Поэтому поведение не включается по умолчанию в `appsettings.json`, а задается compose environment variable.

## Паттерны GoF/GRASP

### GRASP Protected Variations

Connection string и startup migration behavior вынесены в configuration.

Проблема: локальный запуск, Docker Compose и будущий CI/CD будут использовать разные адреса БД и разные правила миграций.

Решение: application code не знает, где находится PostgreSQL. Он читает `ConnectionStrings:PulseRisk` и `Database:ApplyMigrationsOnStartup`.

Почему это не overengineering: это стандартная изменяемая граница deployment-а, а не абстракция ради абстракции.

Та же логика применена к `GSS Encryption Mode=Disable`: это environment-specific настройка драйвера PostgreSQL, а не доменное решение. Она живет в compose connection string и не просачивается в application layer.

### GRASP Indirection

`docker-compose.yml` связывает API и PostgreSQL через имя сервиса `postgres`, а не через host-specific адрес.

Это простая инфраструктурная indirection: API не зависит от `localhost`, случайного IP контейнера или локального порта.

### Template Method на уровне host lifecycle

ASP.NET Core host задает lifecycle приложения.

Мы вставляем миграции между:

```text
builder.Build()
app.Run()
```

Собственный lifecycle framework не создается. Используется существующая точка расширения application startup.

### Отказ от overengineering

В этой главе не добавлены:

- Redis;
- Prometheus;
- Grafana;
- отдельный worker host;
- Kubernetes manifests;
- reverse proxy.

Причина простая: цель главы - минимальный воспроизводимый demo-run API + PostgreSQL. Observability и отдельные процессы появятся позже, когда для них появится отдельная демонстрационная цель.

## Testcontainers hardening

Пока проверялся Docker Compose, был укреплен Testcontainers setup:

- PostgreSQL Testcontainers image выровнен с compose: `postgres:17.4`;
- test host теперь пере-регистрирует `PulseRiskDbContext` напрямую на connection string контейнера;
- тестовый query assertion не фильтрует по `Symbol.Value` внутри LINQ, потому что EF Core не переводит такое выражение.

Это полезный побочный результат главы: Docker окружение позволило прогнать все integration tests реально, без skipped сценариев.

## Локальная проверка

Проверено:

```bash
dotnet build PulseRisk.slnx
docker compose config
docker compose up -d postgres
docker compose ps
docker compose exec -T postgres psql -U pulserisk -d pulserisk -c "select current_database(), current_user;"
docker compose up --build -d
curl http://localhost:5000/api/health
curl http://localhost:5000/swagger/v1/swagger.json
docker compose exec -T postgres psql -U pulserisk -d pulserisk -c "\dt"
dotnet test PulseRisk.slnx
docker compose down --volumes
```

Результаты:

| Проверка | Результат |
| --- | --- |
| Build | passed |
| Compose config | passed |
| PostgreSQL service | started |
| PostgreSQL healthcheck | healthy |
| SQL smoke query | returned `pulserisk / pulserisk` |
| Full Compose build | passed |
| API container | started |
| Health endpoint | `200 OK` |
| Swagger/OpenAPI JSON | `200 OK` |
| EF Core migrations | applied on startup |
| PostgreSQL tables | 9 tables including `__EFMigrationsHistory` |
| Unit tests | `73/73` |
| Integration tests | `9/9` |
| Skipped tests | `0` |

Отчет сохранен:

```text
docs/docker-compose/2026-07-05-local-compose-check.md
docs/docker-compose/2026-07-06-full-compose-smoke.md
```

## Наблюдение по registry/NuGet

Первый полный запуск:

```bash
docker compose up --build -d
```

занял заметное время. Сначала Docker скачивал .NET 10 base images:

```text
mcr.microsoft.com/dotnet/sdk:10.0
mcr.microsoft.com/dotnet/aspnet:10.0
```

Затем `dotnet restore` внутри build container несколько раз получал 60-секундные NuGet timeout warnings. Команда в итоге завершилась успешно. Это важное практическое наблюдение: полный container build зависит не только от корректности Dockerfile, но и от registry/package-feed доступности. После успешного restore Docker layer cache делает повторные сборки быстрее, пока project files не меняются.

## Что изменилось в репозитории

- Добавлен `Dockerfile`.
- Добавлен `.dockerignore`.
- Добавлен `docker-compose.yml`.
- Добавлен startup extension `ApplyPulseRiskDatabaseMigrationsAsync`.
- `Program.cs` применяет миграции на старте при включенном флаге.
- В compose connection string добавлено `GSS Encryption Mode=Disable` для чистых Npgsql logs без Kerberos/GSS probe.
- Добавлены документы `docs/docker-compose`.
- Добавлена глава книги `31-docker-compose.md`.
- Обновлены `README.md`, `ARCHITECTURE.md`, `DEVELOPMENT_PLAN.md`, `book/progress.md`.
- Укреплена Testcontainers factory.

## Частые ошибки

- Публиковать PostgreSQL на `localhost:5432` без необходимости и конфликтовать с локальной БД.
- Включать migrations-on-startup без отдельного флага.
- Делать Swagger недоступным в demo Compose.
- Считать `depends_on` полной гарантией готовности БД без healthcheck.
- Коммитить `.env` с секретами.
- Тащить `book/` и `docs/` внутрь API image.
- Считать registry timeout ошибкой приложения.
- Оставлять шумные driver warnings в demo logs, если их можно убрать явной настройкой.

## Вопросы для интервью

- Почему API подключается к PostgreSQL по имени сервиса `postgres`?
- Почему PostgreSQL port не опубликован на host?
- Почему миграции на старте включаются флагом?
- Что проверяет `docker compose config`?
- Почему `depends_on` использует `service_healthy`?
- Почему в connection string отключен GSS Encryption Mode?
- Чем demo Compose отличается от production deployment?
- Какие сервисы разумно добавить следующими?
- Как бы вы вынесли workers в отдельный process?

## Чек-лист главы

- [x] `Dockerfile` добавлен.
- [x] `.dockerignore` добавлен.
- [x] `docker-compose.yml` добавлен.
- [x] Сервис `pulserisk-api` описан.
- [x] Сервис `postgres` описан.
- [x] Named volume для PostgreSQL добавлен.
- [x] Connection string передается через environment variables.
- [x] `ASPNETCORE_URLS=http://+:5000` задан.
- [x] Startup migrations включаются через флаг.
- [x] PostgreSQL healthcheck добавлен.
- [x] `docker compose config` проверен.
- [x] PostgreSQL service проверен.
- [x] Integration tests проходят на реальном Docker/PostgreSQL.
- [x] Полный `docker compose up --build` с API image проверен.
- [x] Swagger доступен из API container.
