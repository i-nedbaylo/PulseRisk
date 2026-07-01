# Глава 2. Анализ требований к демонстрируемым компетенциям

## Цель главы

Разобрать набор целевых компетенций и превратить его в конкретные требования к PulseRisk.

## Демонстрируемые компетенции

Исходный перечень ожиданий сохранен в нейтральном виде в `PROJECT_REQUIREMENTS.md`. В этой главе требования к компетенциям переводятся в технические решения проекта.

## Черновая таблица соответствия

| Требование к компетенции | Как это показывает PulseRisk |
| --- | --- |
| C#/.NET Core | Проект на .NET 10 и ASP.NET Core Web API |
| Высокие нагрузки | Симулятор котировок, load scenario, bounded channels |
| Multithreading | `BackgroundService`, `Channel<T>`, `CancellationToken`, async pipeline |
| PostgreSQL | Нормализованная схема, индексы, транзакции, EXPLAIN ANALYZE |
| Architecture | Модульный монолит, layered architecture, GoF/GRASP без overengineering |
| Unit-тесты | xUnit/NUnit-тесты домена и risk rules |
| GitLab CI/CD | `.gitlab-ci.yml` со стадиями restore/build/test/docker |
| Fintech | Сделки, позиции, котировки, risk metrics, лимиты и алерты |

## Чек-лист главы

- [ ] Требования к демонстрируемым компетенциям разобраны.
- [ ] Для каждого требования есть реализация или planned milestone.
- [ ] Глава не обещает production trading system.
