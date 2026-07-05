# Full Docker Compose Smoke Test

Дата проверки: 2026-07-06.

Цель проверки - подтвердить, что полный demo Compose контур `pulserisk-api + postgres` собирается, запускается, применяет EF Core migrations на чистую PostgreSQL и отдает health/OpenAPI endpoints через опубликованный порт API.

## Команды

```bash
docker compose up --build -d
docker compose ps
curl http://localhost:5000/api/health
curl http://localhost:5000/swagger/v1/swagger.json
docker compose exec -T postgres psql -U pulserisk -d pulserisk -c "\dt"
docker compose logs --tail 80 pulserisk-api
```

## Результаты

| Проверка | Результат |
| --- | --- |
| `docker compose up --build -d` | passed |
| `pulserisk-api` | `Up`, порт `5000:5000` опубликован |
| `postgres` | `Up`, healthcheck `healthy` |
| `/api/health` | `200 OK`, `status=Healthy` |
| `/swagger/v1/swagger.json` | `200 OK`, OpenAPI document returned |
| EF Core migrations | applied on startup |
| PostgreSQL tables | `__EFMigrationsHistory`, `clients`, `instruments`, `positions`, `quotes`, `risk_alerts`, `risk_rules`, `trades`, `trading_accounts` |

## Наблюдения

Первый полный build долго выполнял `dotnet restore` внутри container build из-за нестабильных ответов NuGet. Несколько пакетов получили 60-секундные timeout warnings, но restore завершился успешно, после чего Docker layer cache делает повторные сборки быстрее при неизменных project files.

Для контейнерного connection string добавлен параметр:

```text
GSS Encryption Mode=Disable
```

Он отключает GSS-API probe в Npgsql 10 для локального demo-контура, где Kerberos/GSS не используется. Это убирает безвредный шум в Linux-container logs про `libgssapi_krb5.so.2` и оставляет startup logs понятнее для демонстрации.

## Вывод

Полный Docker Compose smoke test пройден: API image собирается, оба сервиса стартуют, API отвечает через `localhost:5000`, Swagger/OpenAPI доступен, миграции применяются к PostgreSQL автоматически при включенном флаге `Database__ApplyMigrationsOnStartup=true`.
