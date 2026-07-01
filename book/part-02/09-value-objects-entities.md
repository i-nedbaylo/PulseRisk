# Глава 9. Реализуем value objects и entities

## Цель главы

Реализовать первые доменные типы PulseRisk: enums, value objects, entities и модели risk evaluation.

## Демонстрируемые компетенции

Глава показывает аккуратное моделирование домена без привязки к инфраструктуре. Объекты создаются в валидном состоянии, а бизнес-инварианты проверяются на входе.

## Что сделано в коде

Добавлены enums:

- `ClientStatus`;
- `CurrencyCode`;
- `TradeSide`;
- `RiskRuleType`;
- `RiskAlertType`;
- `RiskSeverity`.

Добавлены value objects:

- `Symbol`;
- `Price`;
- `Volume`;
- `Money`;
- `Percentage`.

Добавлены entities:

- `Client`;
- `TradingAccount`;
- `Instrument`;
- `Trade`;
- `Position`;
- `Quote`;
- `RiskRule`;
- `RiskAlert`.

Добавлены расчетные модели:

- `RiskEvaluationContext`;
- `RiskMetricSnapshot`;
- `RiskRuleEvaluationResult`.

## Паттерны GoF/GRASP

- **Creator**: `Trade.Create`, `Client.Create`, `Position.CreateEmpty` создают объекты с корректными начальными значениями.
- **Information Expert**: `Quote` сам проверяет правило `Bid <= Ask`, `TradingAccount` защищает валюту счета, `RiskAlert` знает, активен ли он.
- **High Cohesion**: value objects отвечают только за свои инварианты, entities - за состояние доменных объектов.

GoF-паттерны здесь не вводятся: пока нет алгоритмической вариативности, которую нужно закрывать Strategy, Factory Method или другими GoF-абстракциями.

## Проверка результата

Проверка выполняется командой:

```bash
dotnet test PulseRisk.slnx
```

## Чек-лист главы

- [x] Enums добавлены.
- [x] Value objects добавлены.
- [x] Entities добавлены.
- [x] Risk evaluation модели добавлены.

