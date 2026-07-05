# Local Docker Compose Check

Дата проверки: `2026-07-05`.

## Что проверялось

Цель проверки - убедиться, что Docker Compose конфигурация синтаксически валидна, PostgreSQL-сервис стартует с healthcheck, а API image готов к сборке через multi-stage `Dockerfile`.

## Команды

```bash
dotnet build PulseRisk.slnx
docker compose config
docker compose up -d postgres
docker compose ps
docker compose exec -T postgres psql -U pulserisk -d pulserisk -c "select current_database(), current_user;"
docker compose down --volumes
dotnet test PulseRisk.slnx
```

## Результаты

| Проверка | Результат |
| --- | --- |
| `dotnet build PulseRisk.slnx` | passed, warnings `0`, errors `0` |
| `docker compose config` | passed |
| `docker compose up -d postgres` | PostgreSQL container started |
| PostgreSQL healthcheck | `healthy` |
| `psql` smoke query | returned `pulserisk / pulserisk` |
| `dotnet test PulseRisk.slnx` | passed: unit `73/73`, integration `9/9`, skipped `0` |

## Полная проверка API image

Полный запуск:

```bash
docker compose up --build -d
```

на текущей машине не завершился из-за внешнего registry/network сбоя при скачивании base images:

```text
TLS handshake timeout
```

Ошибки были получены при обращении к Docker Hub/MCR:

- `postgres:17-alpine` до переключения compose на локально доступный `postgres:17.4`;
- `mcr.microsoft.com/dotnet/sdk:10.0`;
- `mcr.microsoft.com/dotnet/aspnet:10.0`.

После восстановления доступа к registry нужно повторить:

```bash
docker compose up --build -d
curl http://localhost:5000/api/health
curl http://localhost:5000/swagger/v1/swagger.json
docker compose down --volumes
```

## Вывод

Compose-файл валиден, PostgreSQL service проверен, EF/Testcontainers сценарии теперь проходят на локальном Docker. Полный API container smoke test остается внешне заблокирован скачиванием .NET 10 base images.
