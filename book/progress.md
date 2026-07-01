# PulseRisk: карта прогресса книги и проекта

| Глава | Milestone кода | Git tag | Статус |
| --- | --- | --- | --- |
| 1. Зачем мы строим PulseRisk | Контекст проекта описан | `chapter-01-project-purpose` | Черновик |
| 2. Анализ требований к компетенциям | Требования сопоставлены с проектом | `chapter-02-requirements-fit` | Черновик |
| 3. Формулируем техническое задание | MVP scope и критерии приемки зафиксированы | `chapter-03-requirements` | Черновик |
| 4. Выбираем архитектурный стиль | `ARCHITECTURE.md` согласован с проектом | `chapter-04-architecture` | Черновик |
| 5. Планируем путь разработки | `DEVELOPMENT_PLAN.md` согласован с книгой | `chapter-05-development-plan` | Черновик |
| 6. Создаем solution и структуру репозитория | Solution и проекты созданы | `chapter-06-solution-skeleton` | Выполнено |
| 7. Настраиваем .NET 10, analyzers и стиль кода | Repo standards добавлены | `chapter-07-repo-standards` | Выполнено |
| 8. Проектируем доменную модель | Модель описана перед реализацией | `chapter-08-domain-design` | Выполнено |
| 9. Реализуем value objects и entities | Domain entities/value objects добавлены | `chapter-09-domain-model` | Выполнено |
| 10. Пишем первые unit-тесты домена | Первые unit-тесты проходят | `chapter-10-domain-tests` | Выполнено |
| 11. Подключаем PostgreSQL и EF Core | DbContext и PostgreSQL зависимости добавлены | `chapter-11-postgresql-efcore` | Выполнено |
| 12. Проектируем схему БД и миграции | Initial migration создана | `chapter-12-database-schema` | Выполнено |
| 13. Реализуем clients, accounts и instruments | Базовые use cases работают | `chapter-13-reference-data-api` | Выполнено |
| 14. Реализуем обработку сделок | `POST /api/trades` работает | `chapter-14-trade-processing` | Выполнено |
| 15. Обновляем позиции транзакционно | Trade + position транзакция работает | `chapter-15-positions-transaction` | Выполнено |
| 16. Добавляем REST API и Swagger | Swagger показывает основные endpoints | `chapter-16-api-swagger` | Не начато |
| 17. Пишем integration tests через Testcontainers | Первый PostgreSQL integration test проходит | `chapter-17-integration-tests` | Не начато |
| 18. Проектируем internal event pipeline | Event contracts и channel abstractions готовы | `chapter-18-event-pipeline` | Выполнено |
| 19. Реализуем bounded channels и backpressure | Bounded channel behavior протестирован | `chapter-19-backpressure` | Выполнено |
| 20. Создаем Market Data Simulator | Quote simulator генерирует события | `chapter-20-market-simulator` | Не начато |
| 21. Пишем batch writer для котировок | Batch insert котировок работает | `chapter-21-quote-batch-writer` | Не начато |
| 22. Проектируем Risk Engine | Risk Engine contracts готовы | `chapter-22-risk-engine-design` | Не начато |
| 23. Реализуем risk rule strategies | Risk strategies покрыты тестами | `chapter-23-risk-strategies` | Не начато |
| 24. Создаем risk alerts и deduplication | Alerts создаются без лавины дублей | `chapter-24-risk-alerts` | Не начато |
| 25. Добавляем quote-driven risk evaluation | Котировки влияют на risk metrics | `chapter-25-quote-risk-evaluation` | Не начато |
| 26. Покрываем бизнес-логику unit-тестами | Обязательные unit-тесты готовы | `chapter-26-unit-test-coverage` | Не начато |
| 27. Укрепляем integration tests | Сквозные сценарии покрыты | `chapter-27-integration-coverage` | Не начато |
| 28. Оптимизируем PostgreSQL-запросы | EXPLAIN ANALYZE reports добавлены | `chapter-28-postgresql-performance` | Не начато |
| 29. Добавляем нагрузочный сценарий | Load scenario формирует отчет | `chapter-29-load-scenario` | Не начато |
| 30. Измеряем производительность и аллокации | BenchmarkDotNet report добавлен | `chapter-30-benchmarks` | Не начато |
| 31. Упаковываем проект в Docker Compose | `docker compose up --build` работает | `chapter-31-docker-compose` | Не начато |
| 32. Настраиваем GitLab CI/CD | Pipeline описан и запускает проверки | `chapter-32-gitlab-ci` | Не начато |
| 33. Пишем README и финальную документацию | README готов к внешнему показу | `chapter-33-readme-docs` | Не начато |
| 34. Готовим проект к демонстрации на интервью | Interview script готов | `chapter-34-interview-demo` | Не начато |
