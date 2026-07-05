# Docker Compose checks

В этой папке фиксируются локальные проверки Docker Compose окружения PulseRisk.

Compose-конфигурация предназначена для демонстрационного запуска:

- `pulserisk-api` собирается из `Dockerfile`;
- `postgres` использует `postgres:17.4`;
- данные PostgreSQL хранятся в named volume `postgres-data`;
- API получает connection string через environment variables;
- миграции EF Core применяются на старте только при `Database__ApplyMigrationsOnStartup=true`;
- Swagger включается через `ASPNETCORE_ENVIRONMENT=Development`.

## Основные команды

```bash
docker compose config
docker compose up --build
docker compose down --volumes
```

После запуска API:

```bash
curl http://localhost:5000/api/health
curl http://localhost:5000/swagger/v1/swagger.json
```

PostgreSQL не публикуется на host port, чтобы не конфликтовать с локальной БД. Проверять его можно через compose network:

```bash
docker compose exec postgres psql -U pulserisk -d pulserisk -c "select current_database(), current_user;"
```
