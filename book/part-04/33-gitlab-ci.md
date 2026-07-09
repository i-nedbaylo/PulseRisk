# Глава 33. Настраиваем GitLab CI/CD

## Цель главы

Добавить воспроизводимый GitLab pipeline, который показывает культуру DevOps для backend-проекта: зависимости восстанавливаются отдельно, код собирается в `Release`, unit и integration tests разделены, артефакты сохраняются, Docker image проверяется отдельной стадией.

После глав 31 и 32 проект уже запускался через Docker Compose и получил план GitHub Actions. Теперь нужно перенести основные проверки еще и в GitLab pipeline, чтобы показать переносимость CI/CD-подхода между платформами.

## Демонстрируемые компетенции

Глава показывает:

- разбиение pipeline на стадии;
- NuGet cache;
- test artifacts;
- coverage artifacts в Cobertura format;
- Docker-in-Docker для Testcontainers и Docker build;
- осознанное разделение быстрых unit tests и инфраструктурных integration tests;
- понимание ограничений CI runner-а.

## Структура pipeline

В `.gitlab-ci.yml` добавлены стадии:

```yaml
stages:
  - restore
  - build
  - unit_tests
  - integration_tests
  - static_analysis
  - docker_build
```

Порядок выбран так, чтобы feedback шел от дешевого к дорогому:

1. Сначала проверяем, что зависимости восстанавливаются.
2. Затем собираем весь solution.
3. После этого запускаем unit tests.
4. Потом запускаем integration tests, которым нужен Docker.
5. Отдельно проверяем форматирование.
6. В конце собираем Docker image.

## Restore и NuGet cache

Pipeline задает:

```yaml
NUGET_PACKAGES: ".nuget/packages"
```

и кеширует:

```yaml
.nuget/packages/
```

Это делает NuGet cache явной частью workspace. Runner не зависит от глобального состояния пользователя и не пытается писать в домашний каталог.

## Build

Build job выполняет:

```bash
dotnet build PulseRisk.slnx --configuration "$CONFIGURATION" --no-restore
```

`--no-restore` используется намеренно: restore уже был отдельной стадией. Так pipeline проверяет, что стадии действительно связаны артефактами и кешем, а не каждая работа делает все заново.

## Unit tests

Unit tests запускаются отдельно:

```bash
dotnet test tests/PulseRisk.UnitTests/PulseRisk.UnitTests.csproj \
  --configuration "$CONFIGURATION" \
  --no-build \
  --logger "trx;LogFileName=unit-tests.trx" \
  --results-directory artifacts/test-results/unit \
  --collect:"XPlat Code Coverage" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

Артефакты:

- TRX test result;
- `coverage.cobertura.xml`.

Почему не запускать сразу весь solution? Потому что unit tests дают быстрый сигнал и не требуют Docker. Это важная грань: быстрые проверки не должны зависеть от инфраструктуры.

## Integration tests

Integration tests требуют Docker, потому что PostgreSQL-сценарии используют Testcontainers.

Job подключает service:

```yaml
services:
  - name: docker:28.4-dind
    alias: docker
```

и задает:

```yaml
DOCKER_HOST: tcp://docker:2375
TESTCONTAINERS_HOST_OVERRIDE: docker
TESTCONTAINERS_RYUK_DISABLED: "true"
```

Перед тестами job устанавливает Docker CLI и выполняет:

```bash
docker pull postgres:17.4
```

Это связано с текущим preflight в тестах: `DockerAvailability.IsDockerImageAvailable("postgres:17.4")` проверяет, что image уже есть локально. В CI это делает интеграционные тесты строгими: если Docker недоступен или image не подтягивается, job не должен тихо превращаться в skipped.

## Static analysis

Статическая проверка сейчас минимальная:

```bash
dotnet format analyzers PulseRisk.slnx --verify-no-changes --verbosity minimal --no-restore
```

Это не заменяет полноценные security scanners, но дает первый CI guardrail по Roslyn analyzers.

Почему не `dotnet format` целиком? В текущем репозитории исторически смешаны LF/CRLF и encoding у части generated migration files. Полная whitespace-проверка сейчас превратила бы CI-шаг в большой механический diff по line endings. Это отдельная техническая уборка, а не цель главы про CI.

## Docker build

Последняя стадия выполняет:

```bash
docker build --pull -t "pulserisk-api:$CI_COMMIT_SHORT_SHA" .
```

Image пока не публикуется в registry. На этом этапе цель проще: доказать, что Dockerfile собирается в чистом CI-контексте. Публикацию можно добавить позже отдельным правилом для protected branch/tag.

## Паттерны GoF/GRASP

### GRASP Protected Variations

Pipeline закрывает изменчивость окружения через переменные:

- `NUGET_PACKAGES`;
- `CONFIGURATION`;
- `DOCKER_HOST`;
- `TESTCONTAINERS_HOST_OVERRIDE`.

Код приложения не меняется, когда он запускается локально, в Docker Compose или GitLab CI.

### GRASP Indirection

Docker-in-Docker service получает alias `docker`, а tests обращаются к Docker daemon через `DOCKER_HOST=tcp://docker:2375`. Это инфраструктурная прослойка между job container и daemon.

### Pipeline как Chain of Responsibility, но без собственной реализации

Стадии CI похожи на цепочку проверок: restore -> build -> tests -> analysis -> docker build. Но мы не реализуем GoF Chain of Responsibility в коде. GitLab уже предоставляет механизм pipeline stages, поэтому достаточно declarative YAML.

### Отказ от overengineering

В первом CI варианте не добавлены:

- push Docker image в registry;
- release tags;
- scheduled benchmarks;
- deployment;
- отдельная матрица SDK/runtime.

Причина: для текущего уровня проекта важнее надежно закрепить базовые проверки. Расширять pipeline нужно после первого успешного запуска в реальном GitLab runner-е.

## Ограничение проверки

Локально можно проверить команды, которые выполняет pipeline: `dotnet build`, `dotnet test`, `dotnet format`, `docker build`.

Но полный факт "pipeline проходит на push/MR" подтверждается только GitLab runner-ом. Поэтому пункты проверки push и merge request остаются открытыми до подключения репозитория к GitLab.

## Что изменилось в репозитории

- Добавлен `.gitlab-ci.yml`.
- Добавлен `docs/ci/README.md`.
- Добавлена глава `book/part-04/33-gitlab-ci.md`.
- Обновлены `README.md`, `ARCHITECTURE.md`, `DEVELOPMENT_PLAN.md`, `book/progress.md`.

## Вопросы для интервью

- Почему unit tests и integration tests разделены?
- Зачем нужен NuGet cache внутри project workspace?
- Почему integration job требует Docker-in-Docker?
- Почему Docker image пока только собирается, но не публикуется?
- Что должно быть настроено на GitLab runner-е для Testcontainers?
- Чем локальный Docker Compose smoke test отличается от CI Docker build?

## Чек-лист главы

- [x] `.gitlab-ci.yml` добавлен.
- [x] Stage `restore` добавлен.
- [x] Stage `build` добавлен.
- [x] Stage `unit_tests` добавлен.
- [x] Stage `integration_tests` добавлен.
- [x] Stage `static_analysis` добавлен.
- [x] Stage `docker_build` добавлен.
- [x] NuGet cache настроен.
- [x] Test artifacts настроены.
- [x] Coverage artifacts настроены.
- [ ] Pipeline проверен на branch push в GitLab.
- [ ] Pipeline проверен на merge request в GitLab.
