# Глава 32. Настраиваем GitHub Actions CI/CD

## Цель главы

Добавить GitHub Actions workflow, который повторяет базовую инженерную проверку проекта на GitHub: restore, build, unit tests, integration tests, static analysis, Docker build, NuGet cache и test/coverage artifacts.

После главы 31 проект можно запустить через Docker Compose. Теперь GitHub-репозиторий должен получить такой же автоматический контур проверки, чтобы изменения в `main` попадали только через Pull Request и после успешных checks.

## Демонстрируемые компетенции

Глава должна показать:

- перенос CI/CD-практик на GitHub Actions;
- раздельные jobs или steps для restore, build, unit tests, integration tests, static analysis и Docker build;
- NuGet cache через `actions/cache` или `setup-dotnet` cache;
- публикацию TRX и coverage artifacts;
- запуск integration tests с PostgreSQL/Testcontainers;
- связь CI checks с GitHub branch protection.

## План workflow

В репозитории должен появиться файл:

```text
.github/workflows/ci.yml
```

Workflow должен запускаться на:

```yaml
on:
  push:
  pull_request:
```

Минимальный набор проверок:

1. Restore зависимостей.
2. Build solution в `Release`.
3. Unit tests с TRX и Cobertura coverage.
4. Integration tests с Docker/PostgreSQL.
5. Static analysis через `dotnet format analyzers --verify-no-changes`.
6. Docker build API image.

## Branch protection

После первого успешного запуска workflow нужно усилить защиту `main`:

- требовать Pull Request перед merge;
- требовать успешные GitHub Actions checks;
- запретить прямой push в `main`;
- запретить force push и удаление ветки.

Это связывает технический CI с рабочим процессом разработки: новая логика попадает в `main` только через review и автоматические проверки.

## Отличие от GitLab CI

GitHub Actions и GitLab CI решают одну задачу, но используют разные модели:

- GitLab CI описывает pipeline через stages и jobs в `.gitlab-ci.yml`;
- GitHub Actions описывает workflow, jobs и steps в `.github/workflows/*.yml`;
- branch protection на GitHub может напрямую требовать конкретные workflow checks перед merge.

Для PulseRisk важно показать не привязку к одному CI-сервису, а понимание самих проверок: сборка, тесты, анализ, Docker и артефакты.

## Что изменится в репозитории

- Добавлен `.github/workflows/ci.yml`.
- Обновлен `README.md` с упоминанием GitHub Actions CI/CD.
- Защита `main` на GitHub будет обновлена после появления стабильных checks.
- `DEVELOPMENT_PLAN.md` и `book/progress.md` отражают GitHub Actions как отдельный этап перед GitLab CI/CD.

## Вопросы для интервью

- Почему GitHub Actions добавлен отдельной главой, а не заменяет GitLab CI?
- Какие checks должны блокировать merge в `main`?
- Почему integration tests требуют отдельного внимания к Docker/Testcontainers?
- Чем artifacts отличаются от logs?
- Когда стоит добавлять Docker image publish в registry?

## Чек-лист главы

- [x] `.github/workflows/ci.yml` добавлен.
- [x] Restore/NuGet cache настроены.
- [x] Build job добавлен.
- [x] Unit tests публикуют TRX и coverage artifacts.
- [x] Integration tests job добавлен.
- [x] Static analysis job добавлен.
- [x] Docker build job добавлен.
- [ ] Workflow проверен на push.
- [ ] Workflow проверен на pull request.
- [ ] Required status checks подключены в GitHub branch protection для `main`.
