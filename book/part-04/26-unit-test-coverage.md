# Глава 26. Покрываем бизнес-логику unit-тестами

## Цель главы

Усилить unit-тестовое покрытие Risk Engine и закрыть edge cases, которые легко пропустить при happy-path разработке.

К этому моменту проект уже умеет:

- принимать сделки;
- обновлять позиции;
- сохранять котировки batch-ами;
- запускать risk evaluation от позиции и от котировок;
- применять первые risk strategies;
- создавать и deduplicate risk alerts.

Теперь нужно проверить не только "правило сработало", но и "система корректно ничего не делает", когда условий для alert-а нет.

## Демонстрируемые компетенции

Глава показывает:

- осознанный выбор тестового уровня;
- проверку negative paths;
- тестирование orchestration без запуска `BackgroundService`;
- фиксацию business edge cases;
- boundary tests для threshold-логики;
- изоляцию тестов от PostgreSQL там, где база не нужна;
- применение GRASP/GoF решений через тестируемые границы.

## Что проверяем

Добавляем и усиливаем тесты для сценариев:

- latest quote отсутствует;
- active risk rules отсутствуют;
- rule disabled и не участвует в проверке;
- active rule есть, но strategy еще не реализована;
- triggered result без сообщения не превращается в alert;
- `MaxLoss` срабатывает ровно на threshold;
- `MarginLevelWarning` срабатывает ровно на threshold.

Эти сценарии важны, потому что Risk Engine работает в фоне. Ошибка в background flow часто проявляется не как красивый HTTP 400, а как лишний alert, потерянный alert или шум в логах.

## Паттерны GoF/GRASP и тестируемость

### Strategy

`IRiskRuleStrategy` позволяет тестировать каждое правило изолированно.

Например, `MaxLossRuleStrategyTests` проверяет только сравнение `FloatingPnL <= threshold`, не поднимая repositories, channels или worker.

### Simple Factory / Creator

`RiskAlertFactoryTests` проверяет, что alert создается только из валидного triggered result.

Это тестирует GRASP Creator: объект создается в одном месте и защищает инварианты.

### Indirection

`RiskEvaluationProcessorTests` используют fake repositories и fake event writer.

Processor тестируется как orchestration-компонент, но без EF Core и без настоящих channels. Это возможно из-за application ports:

- `IRiskAlertRepository`;
- `IRiskRuleRepository`;
- `ILatestQuoteReader`;
- `IEventWriter<RiskAlertRaisedEvent>`.

### Pure Fabrication

`QuoteRiskEvaluationDispatcher` и `RiskEvaluationProcessor` не являются доменными сущностями. Они существуют ради orchestration.

Их удобно тестировать отдельно от hosted worker lifecycle.

## Реализация шаг за шагом

### Шаг 1. Missing latest quote

`RiskEvaluationContextBuilder` уже умел возвращать skipped result, если latest quote отсутствует.

В этой главе добавлен processor-level тест:

```text
ProcessAsync_WhenLatestQuoteDoesNotExist_ShouldSkipWithoutPersistingAlerts
```

Он проверяет, что skipped preparation не приводит к:

- созданию `RiskAlert`;
- вызову `SaveChanges`;
- публикации `RiskAlertRaisedEvent`.

Это важнее простого "builder вернул skip": мы фиксируем поведение всего background flow.

### Шаг 2. No active rules

Если active rules нет, Risk Engine должен построить context, но не создать alert.

Тест:

```text
ProcessAsync_WhenNoActiveRulesExist_ShouldNotPersistAlerts
```

Такой сценарий может случиться в админской конфигурации или в тестовой среде, где все rules отключены.

### Шаг 3. Disabled rule

Контракт `IRiskRuleRepository.ListEnabledAsync(...)` означает, что processor получает только enabled rules.

В unit fixture fake repository теперь ведет себя так же: фильтрует `rule.IsEnabled`.

Тест:

```text
ProcessAsync_WhenRuleIsDisabled_ShouldNotPersistAlert
```

Он защищает бизнес-ожидание: disabled rule не создает alert даже если метрики нарушают threshold.

### Шаг 4. Unsupported active rule

Seed уже содержит `PriceSpikeDetection` и `HighFrequencyTradingActivity`, но соответствующие strategies пока не реализованы.

Тест:

```text
ProcessAsync_WhenActiveRuleHasNoStrategy_ShouldSkipWithoutPersistingAlerts
```

Он фиксирует мягкое поведение: active rule без strategy логируется и пропускается, а worker не падает.

Это осознанный Protected Variations сценарий: доменная конфигурация может появиться раньше реализации всех алгоритмов.

### Шаг 5. Blank alert message

`RiskRuleEvaluationResult.NotTriggered()` содержит nullable-поля, потому что not triggered result не должен иметь alert data.

Но triggered result должен быть полным.

`RiskAlertFactoryTests` теперь проверяет:

```text
Create_WhenTriggeredResultHasBlankMessage_ShouldThrow
```

Alert без сообщения бесполезен для оператора и downstream-потребителей, поэтому factory не должен его создавать.

### Шаг 6. Threshold boundaries

Для risk rules граница сравнения важна.

Добавлены проверки:

- `MaxLoss` срабатывает при `FloatingPnL == threshold`;
- `MarginLevelWarning` срабатывает при `MarginLevel == threshold`.

Это защищает от случайной замены `<=` на `<`.

`MaxExposure` уже был покрыт обратной границей: exposure на threshold не считается превышением.

## Почему эти тесты unit-level

Каждый из добавленных сценариев проверяет бизнес-решение, а не работу PostgreSQL.

Поэтому unit-тесты лучше integration-тестов:

- быстрее выполняются;
- не зависят от Docker;
- точнее показывают причину падения;
- позволяют моделировать negative paths без сложной подготовки БД;
- хорошо защищают refactoring.

Integration tests нужны дальше, но для другого вопроса:

```text
Работает ли весь путь через API/EF/PostgreSQL/worker вместе?
```

Это будет следующая глава.

## Проверка результата

Команда:

```bash
dotnet test PulseRisk.slnx
```

Результат текущей итерации:

- unit-тесты: `69/69`;
- integration tests: `6/8`;
- skipped integration tests: `2/8`.

Оба skipped tests зависят от Docker/Testcontainers PostgreSQL.

## Что изменилось в репозитории

- Усилен `RiskEvaluationProcessorTests`.
- Добавлены сценарии missing quote, no active rules, disabled rule и unsupported strategy.
- Усилен `RiskAlertFactoryTests`.
- Добавлены boundary tests для `MaxLossRuleStrategy` и `MarginLevelWarningRuleStrategy`.
- Обновлены `DEVELOPMENT_PLAN.md`, `ARCHITECTURE.md`, `README.md`, `book/progress.md`.

## Почему не добавляем PriceSpike/HFT tests сейчас

`PriceSpikeDetectionRuleStrategy` и `HighFrequencyTradingActivityRuleStrategy` пока не реализованы.

Писать тесты до появления входного контекста было бы не тестированием, а гаданием об API.

Для этих правил нужен дополнительный context:

- история цен для price spike;
- окно торговой активности для high frequency activity.

Когда появятся эти данные, стратегии и тесты будут добавлены отдельным срезом.

## Частые ошибки

- Покрывать только happy path.
- Проверять disabled rule только в UI/API, но не в Risk Engine flow.
- Падать на unsupported active rule.
- Создавать alert при skipped evaluation.
- Не тестировать equality на threshold.
- Тащить PostgreSQL в тест, где достаточно fake port-а.

## Вопросы для интервью

- Почему missing latest quote должен приводить к skip, а не exception?
- Где проходит граница ответственности `IRiskRuleRepository` и processor-а для disabled rules?
- Почему unsupported rule type не должен падать?
- Почему `MaxLoss` использует `<=`, а `MaxExposure` использует `>`?
- Какие сценарии лучше оставить integration-тестам?
- Как unit-тесты помогают избежать overengineering?

## Чек-лист главы

- [x] Missing latest quote проверяется на уровне processor flow.
- [x] No active rules не создает alert.
- [x] Disabled rule не создает alert.
- [x] Unsupported active rule пропускается без падения worker-а.
- [x] Blank alert message не проходит через factory.
- [x] `MaxLoss` equality threshold покрыт тестом.
- [x] `MarginLevelWarning` equality threshold покрыт тестом.
- [x] Тесты проходят.
- [x] Документы обновлены.
