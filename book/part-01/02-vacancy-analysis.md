# Глава 2. Анализ вакансии IndigoSoft

## Цель главы

Разобрать вакансию и превратить ее формулировки в конкретные требования к PulseRisk.

## Связь с вакансией

Исходный текст вакансии лежит в файле `Вакансия IndigoSoft.md`. В этой главе требования вакансии переводятся в технические решения проекта.

## Черновая таблица соответствия

| Требование вакансии | Как это показывает PulseRisk |
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

- [ ] Требования вакансии разобраны.
- [ ] Для каждого требования есть реализация или planned milestone.
- [ ] Глава не обещает production trading system.

