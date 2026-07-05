# GitLab CI/CD

Папка фиксирует решения и локальные проверки, связанные с pipeline.

Текущий pipeline описан в `.gitlab-ci.yml` и содержит стадии:

- `restore`;
- `build`;
- `unit_tests`;
- `integration_tests`;
- `static_analysis`;
- `docker_build`.

## Что проверяет pipeline

- `dotnet restore` с NuGet cache в `.nuget/packages`;
- `dotnet build` в `Release`;
- unit tests с TRX и Cobertura artifacts;
- integration tests с Testcontainers PostgreSQL через Docker-in-Docker;
- `dotnet format analyzers --verify-no-changes`;
- `docker build` API image.

## Требования к GitLab runner

Integration tests и Docker build jobs требуют runner с Docker-in-Docker. Для Docker executor runner должен быть настроен как privileged, иначе DinD service не сможет поднимать контейнеры для Testcontainers и `docker build`.

Integration job заранее выполняет:

```bash
docker pull postgres:17.4
```

Это нужно потому, что тестовая инфраструктура проекта проверяет наличие PostgreSQL image локально перед запуском Testcontainers-сценариев.

## Что еще нужно подтвердить

Локально можно проверить команды `dotnet build`, `dotnet test`, `dotnet format` и `docker build`. Полный статус GitLab pipeline можно подтвердить только после push в GitLab и запуска pipeline на branch или merge request.
