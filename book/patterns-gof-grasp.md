# PulseRisk: GoF и GRASP в книге и коде

Этот файл задает правило для всей книги: **паттерн упоминается только там, где он решает конкретную проблему проекта**.

Один из целевых навыков проекта - глубокое понимание GoF, GRASP и умение избегать overengineering. Поэтому PulseRisk не должен превращаться в каталог паттернов. Наоборот, книга должна показывать инженерную дисциплину: где абстракция нужна, где достаточно прямого кода, а где паттерн пока лучше не вводить.

## Формат описания паттерна в главах

Когда в главе появляется архитектурный или проектный паттерн, его нужно описывать по одному шаблону:

- **Название:** GoF/GRASP/Enterprise/Application pattern.
- **Место применения:** конкретный модуль, класс или use case.
- **Проблема:** какая боль появляется без паттерна.
- **Решение:** как паттерн меняет код.
- **Почему это не overengineering:** почему простого решения уже недостаточно или почему абстракция ограничена.
- **Альтернатива:** что было бы проще, сложнее или хуже.
- **Проверка:** тест, benchmark, dependency check, диаграмма или code review question.

## GRASP

| GRASP-паттерн | Где применяем | Какую проблему решает | Почему не overengineering |
| --- | --- | --- | --- |
| Information Expert | `Position`, `PositionCalculator`, `PnLCalculator`, risk metric calculations | Бизнес-формулы не должны расползаться по API, EF mappings и workers | Формулы тестируются без БД и остаются рядом с данными, которые им нужны |
| Creator | Фабрики/методы создания `Trade`, `Position`, `RiskAlert` | Объект должен создаваться в валидном состоянии | Создание инкапсулирует инварианты и не требует сложной иерархии |
| Controller | API controllers и application handlers | HTTP-слой не должен содержать бизнес-логику | Controllers остаются тонкими, orchestration живет в Application layer |
| Low Coupling | Направление зависимостей `Api -> Application -> Domain`, инфраструктура через порты | Домен нельзя связывать с PostgreSQL, ASP.NET Core или workers | Границы нужны для unit-тестов и будущего выноса модулей |
| High Cohesion | Отдельные проекты и сервисы: trade processing, positions, risk, simulator | Один сервис не должен знать все обо всем | Каждый модуль имеет понятную причину изменения |
| Polymorphism | `IRiskRuleStrategy` и реализации risk rules | Набор risk rules растет, большой `switch` становится хрупким | Вариативность уже задана предметной областью |
| Pure Fabrication | Application handlers, repositories, query objects, calculators | Не все обязанности естественно принадлежат доменным сущностям | Эти классы снижают связность и повышают тестируемость |
| Indirection | `IEventWriter<T>`, `IEventReader<T>`, repositories, query objects | Producer не должен зависеть от consumer, Application не должен знать SQL details | Посредник изолирует скорость обработки и инфраструктурные детали |
| Protected Variations | Ports, strategies, options, channel policies | Изменяемые части системы не должны ломать стабильный код | Абстракции ставятся только на границах реальной изменчивости |

## GoF

| GoF-паттерн | Где применяем | Какую проблему решает | Важная оговорка |
| --- | --- | --- | --- |
| Strategy | Risk rules: `MaxExposure`, `MaxLoss`, `MarginLevel`, `PriceSpike`, `HighFrequencyActivity` | Разные алгоритмы проверки лимитов должны расширяться независимо | Это главный GoF-паттерн проекта, потому что вариативность реальна |
| Factory Method / Simple Factory | `RiskAlertFactory`, будущие factories для событий | Создание alert/event должно быть единообразным и валидным | Если нет наследования factory types, честно называем это Simple Factory, а не GoF Factory Method |
| Template Method | `BackgroundService.ExecuteAsync` в workers | Фреймворк задает жизненный цикл worker'а, проект реализует конкретный шаг | Это framework-level применение, а не собственная иерархия ради паттерна |
| Adapter | Infrastructure implementations для application ports | Application работает с контрактами, а PostgreSQL/Dapper/Redis подключаются снаружи | Adapter появляется только на инфраструктурной границе |
| Command | `CreateTradeCommand`, `CreateClientCommand` и handlers | Use case получает явный входной объект и отдельный обработчик | Это application-level command style; undo/redo и command queue не нужны |
| Decorator | Возможный retry/logging/validation pipeline вокруг handlers | Cross-cutting behavior не должен копироваться в каждый handler | Вводить только когда появится повторяющееся поведение |
| Observer | Не используем напрямую в MVP | Событийность решается через Producer-Consumer на `Channel<T>` | Не называем любой event pipeline Observer, если нет подписчиков в GoF-смысле |

## Enterprise/Application patterns

Эти паттерны не являются GoF, но важны для backend-проекта:

- **Repository** - скрывает сохранение агрегатов там, где это снижает связность.
- **Query Object** - концентрирует оптимизированные read-only запросы.
- **Unit of Work** - транзакционная граница trade + position через `DbContext`.
- **Outbox** - optional senior extension для надежной публикации событий после commit.
- **Options pattern** - конфигурация simulator rate, channel capacity, batch size, risk thresholds.
- **Producer-Consumer** - основа quote/risk pipeline через bounded channels.

## Паттерны, которые намеренно не вводим на старте

| Паттерн | Почему не вводим сразу |
| --- | --- |
| Abstract Factory | Нет семейства взаимозаменяемых продуктов |
| Visitor | Risk rules проще выразить стратегиями |
| State | Жизненный цикл сделок и алертов пока прост |
| Chain of Responsibility | ASP.NET middleware уже покрывает pipeline HTTP; для risk rules нужна независимая оценка, а не цепочка остановок |
| Mediator package | На старте handlers можно регистрировать напрямую; внешний mediator добавим только при росте cross-cutting concerns |
| Microservices patterns | Модульный монолит достаточен для учебной версии и лучше показывает транзакции |

## Где это появится в книге

| Главы | Основной фокус паттернов |
| --- | --- |
| 4-5 | Low Coupling, High Cohesion, Protected Variations |
| 8-10 | Information Expert, Creator, Pure Fabrication |
| 11-12 | Unit of Work, Repository, Query Object, Adapter |
| 13-17 | Controller, Command style, validation boundaries |
| 18-21 | Producer-Consumer, Indirection, Template Method |
| 22-25 | Strategy, Polymorphism, Simple Factory, Protected Variations |
| 28-30 | Query Object, Pure Fabrication, Protected Variations, отказ от premature abstraction |
