# CI/CD

Папка фиксирует решения и локальные проверки, связанные с CI/CD pipelines.

## GitHub Actions

GitHub workflow описан в `.github/workflows/ci.yml` и запускается на `push` в
`main`, для pull request в `main` и вручную через `workflow_dispatch`. Такой
набор событий не создает два одинаковых запуска для каждого push в PR-ветку.

Workflow содержит jobs:

- `restore`;
- `build`;
- `unit_tests`;
- `integration_tests`;
- `static_analysis`;
- `docker_build`.

GitHub Actions workflow проверяет:

- `dotnet restore` в locked mode с NuGet cache в `.nuget/packages`;
- `dotnet build` в `Release`;
- unit tests с TRX и Cobertura artifacts;
- integration tests с Testcontainers PostgreSQL;
- `dotnet format analyzers --verify-no-changes`;
- `docker build` API image.

Сторонние Actions закреплены по immutable commit SHA, а читаемый комментарий
рядом с SHA фиксирует major-версию. `.github/dependabot.yml` еженедельно
проверяет обновления GitHub Actions. Длительные jobs ограничены через
`timeout-minutes`, поэтому зависший Docker или внешний package feed не занимает
runner бесконечно.

NuGet lock-файлы хранят точный граф транзитивных зависимостей. Локальный
`dotnet restore` может обновлять их после осознанного изменения
`PackageReference`, а CI использует `RestoreLockedMode` и завершается ошибкой,
если project-файлы и lock-файлы расходятся. Dockerfile также копирует
lock-файлы до restore и выполняет его с `--locked-mode`.

GitHub workflow подтвержден на pull request и push в `main`. Для `main`
подключены required status checks, запрет прямого push, PR flow и требование
одного approving review.

## GitLab CI/CD

GitLab pipeline описан в `.gitlab-ci.yml` и содержит стадии:

- `restore`;
- `build`;
- `unit_tests`;
- `integration_tests`;
- `static_analysis`;
- `docker_build`.

## Что проверяет GitLab pipeline

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

Локально можно проверить команды `dotnet build`, `dotnet test`, `dotnet format`
и `docker build`. GitHub Actions уже подтвержден реальными PR/push checks.
Полный статус GitLab pipeline можно подтвердить только после push в GitLab и
запуска pipeline на branch или merge request.
