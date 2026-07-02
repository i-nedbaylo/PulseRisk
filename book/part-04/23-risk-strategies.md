# Глава 23. Реализуем risk rule strategies

## Цель главы

Подключить первые полиморфные risk rules: `MaxExposure`, `MaxLoss` и `MarginLevelWarning`. Эти правила уже могут работать на данных `RiskEvaluationContext`, который был подготовлен в главе 22.

`PriceSpikeDetection` и `HighFrequencyTradingActivity` пока не реализуем: им нужны дополнительные входные данные, которых еще нет в контексте. Это осознанное ограничение, а не забытый код.

## Демонстрируемые компетенции

Глава показывает:

- применение GoF Strategy по реальной причине;
- resolver вместо большого `switch`;
- чистые unit-тестируемые доменные правила;
- постепенное расширение Risk Engine без overengineering;
- безопасное поведение при active rule, для которого strategy еще не зарегистрирована.

## Что уже есть в проекте

К началу главы уже есть:

- `RiskEvaluationContext`;
- `RiskMetricSnapshot`;
- active `RiskRule` из PostgreSQL;
- `RiskEvaluationProcessor`;
- `RiskEvaluationContextBuilder`;
- `RiskRuleEvaluationResult`;
- enum-ы `RiskRuleType`, `RiskAlertType`, `RiskSeverity`.

Теперь можно перейти от подготовки данных к принятию решений.

## Что сделаем сейчас

Добавим:

- `IRiskRuleStrategy`;
- `MaxExposureRuleStrategy`;
- `MaxLossRuleStrategy`;
- `MarginLevelWarningRuleStrategy`;
- `RiskRuleStrategyResolver`;
- применение strategies в `RiskEvaluationProcessor`;
- unit-тесты для resolver-а и трех правил.

## Проектное решение

Каждая strategy получает:

- `RiskEvaluationContext`;
- `RiskRule`;
- `CancellationToken`.

И возвращает:

- `RiskRuleEvaluationResult.NotTriggered()`;
- или `RiskRuleEvaluationResult.Triggered(...)`.

Контракт:

```csharp
public interface IRiskRuleStrategy
{
    RiskRuleType RuleType { get; }

    ValueTask<RiskRuleEvaluationResult> EvaluateAsync(
        RiskEvaluationContext context,
        RiskRule rule,
        CancellationToken cancellationToken);
}
```

Хотя текущие правила синхронные, метод оставлен `ValueTask`: будущие стратегии могут потребовать асинхронный read model или cache lookup, а текущие реализации не платят за allocation обычного `Task`.

## Паттерны GoF/GRASP

- **Strategy**: каждое правило инкапсулирует свой алгоритм проверки.
- **Polymorphism**: `RiskEvaluationProcessor` не содержит большой `switch` по `RiskRuleType`.
- **Protected Variations**: новые правила добавляются через новую strategy, а не через изменение существующих правил.
- **Information Expert**: strategy работает только с теми метриками, которые ей нужны.
- **High Cohesion**: `MaxExposureRuleStrategy` не знает про margin, а `MarginLevelWarningRuleStrategy` не знает про floating PnL.
- **Indirection**: `RiskRuleStrategyResolver` отделяет processor от конкретного списка классов.

Это не overengineering: вариативность правил уже есть в домене и в таблице `risk_rules`. Strategy здесь не декоративная, а защищает Risk Engine от роста условной логики.

## Реализация шаг за шагом

### Шаг 1. Интерфейс стратегии

`IRiskRuleStrategy` находится в Domain layer, потому что правило - это доменная политика, а не инфраструктурный механизм.

Каждая strategy сообщает, какой `RiskRuleType` она обслуживает:

```csharp
public RiskRuleType RuleType => RiskRuleType.MaxExposureLimit;
```

### Шаг 2. MaxExposureRuleStrategy

Правило срабатывает, когда:

```text
context.NetExposure > rule.ThresholdValue
```

Результат:

- `AlertType = ExposureLimitExceeded`;
- `Severity = rule.Severity`;
- message с exposure, threshold и symbol.

Равенство threshold не считается нарушением: лимит превышен только после выхода за границу.

### Шаг 3. MaxLossRuleStrategy

Seed threshold для max loss хранится отрицательным значением, например `-25000`.

Правило срабатывает, когда:

```text
context.FloatingPnL <= rule.ThresholdValue
```

Если floating PnL `-30000`, а threshold `-25000`, лимит убытка нарушен.

### Шаг 4. MarginLevelWarningRuleStrategy

Правило срабатывает, когда:

```text
context.MarginLevel <= rule.ThresholdValue
```

Например, при threshold `100%` значение `80%` должно создать warning.

### Шаг 5. Resolver

`RiskRuleStrategyResolver` строит dictionary по `RiskRuleType`.

Он не выбрасывает exception для неизвестного rule type. Вместо этого:

```csharp
if (!strategyResolver.TryResolve(rule.RuleType, out var strategy))
{
    skippedRules++;
    logger.LogInformation(...);
    continue;
}
```

Это важно прямо сейчас: seed уже содержит `PriceSpikeDetection` и `HighFrequencyTradingActivity`, а их контекст появится позже.

### Шаг 6. Подключаем processor

`RiskEvaluationProcessor` теперь:

1. Получает prepared context.
2. Проходит по active rules.
3. Находит strategy через resolver.
4. Выполняет strategy.
5. Логирует triggered rule как warning.
6. Логирует итог evaluation: active, triggered, skipped.

Создание `RiskAlert` пока не выполняется. Это следующая глава.

## Проверка результата

Команды:

```bash
dotnet build PulseRisk.slnx
dotnet test PulseRisk.slnx
```

Результат текущей итерации:

- unit-тесты: `53/53`;
- integration tests: `6/8`;
- skipped integration tests: `2/8`.

Оба skipped tests зависят от Docker/Testcontainers PostgreSQL.

## Что изменилось в репозитории

- Добавлен `IRiskRuleStrategy`.
- Добавлены `MaxExposureRuleStrategy`, `MaxLossRuleStrategy`, `MarginLevelWarningRuleStrategy`.
- Добавлен `RiskRuleStrategyResolver`.
- `RiskEvaluationProcessor` применяет registered strategies.
- DI регистрирует стратегии и resolver.
- Добавлены unit-тесты для трех стратегий.
- Добавлены unit-тесты resolver-а.
- Обновлены `DEVELOPMENT_PLAN.md`, `ARCHITECTURE.md`, `README.md`, `book/progress.md`.

## Почему сделали именно так

Самый простой путь - написать `switch`:

```csharp
switch (rule.RuleType)
{
    case RiskRuleType.MaxExposureLimit:
        ...
}
```

Для двух правил это нормально. Но в проекте уже есть пять типов правил, а дальше появятся cooldown, deduplication, trade-frequency и price-spike logic. `switch` быстро превратился бы в объект, который знает все обо всем.

Strategy позволяет добавлять правила по одному и тестировать их изолированно.

## Альтернативы

- **Большой switch в processor-е**: быстрее написать, но хуже расширять.
- **Delegate dictionary вместо классов**: компактно, но хуже читается и тестируется при росте правил.
- **Сразу реализовать все пять правил**: выглядело бы завершеннее, но PriceSpike/HFT требуют другой контекст.
- **Сделать strategies application services**: возможно, если правила начнут ходить в read models; пока это чистая доменная логика.

## Частые ошибки

- Считать threshold equality нарушением там, где нужно именно превышение.
- Хранить max loss threshold положительным числом и путать знак.
- Делать strategy зависимой от EF Core.
- Падать на active rule без зарегистрированной strategy.
- Создавать alert прямо внутри strategy.

## Вопросы для интервью

- Почему здесь уместен GoF Strategy?
- Почему resolver возвращает `false`, а не бросает exception для неподдержанного rule type?
- Почему max loss сравнивается через `<=`?
- Почему strategy возвращает result, а не сразу создает `RiskAlert`?
- Как изменится `RiskEvaluationContext` для price spike detection?
- Когда `ValueTask` в strategy оправдан, а когда был бы лишним?

## Чек-лист главы

- [x] `IRiskRuleStrategy` добавлен.
- [x] `RiskRuleStrategyResolver` добавлен.
- [x] `MaxExposureRuleStrategy` реализован.
- [x] `MaxLossRuleStrategy` реализован.
- [x] `MarginLevelWarningRuleStrategy` реализован.
- [x] `RiskEvaluationProcessor` применяет registered strategies.
- [x] Active rule без strategy не ломает worker.
- [x] Unit-тесты strategies добавлены.
- [x] Unit-тесты resolver-а добавлены.
- [x] Код собирается.
- [x] Проверки выполнены.
