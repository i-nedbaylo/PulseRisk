# Глава 28. Оптимизируем PostgreSQL-запросы

## Цель главы

Добавить в проект первый воспроизводимый performance evidence для PostgreSQL.

До этой главы в PulseRisk уже есть:

- PostgreSQL schema и EF Core migrations;
- индексы для основных таблиц;
- batch insert котировок;
- quote-driven risk evaluation;
- active-alert deduplication;
- integration tests на Testcontainers.

Но архитектурное утверждение "запросы спроектированы под нагрузку" должно подтверждаться не словами, а планами выполнения. Поэтому в этой главе мы:

- создаем тестовый набор данных;
- снимаем `EXPLAIN ANALYZE` для hot-path запросов;
- находим один конкретный query bottleneck;
- добавляем точечный partial index;
- сохраняем отчет до и после индекса;
- описываем выводы в `docs/explain-analyze`.

Это не полноценный load test. Load test появится в следующей главе. Здесь фокус только на PostgreSQL planner, индексах и доказуемом подходе к оптимизации.

## Демонстрируемые компетенции

Глава показывает:

- PostgreSQL indexes, partial indexes и planner basics;
- умение читать `EXPLAIN ANALYZE`;
- разделение hot-path запросов по сценариям;
- EF Core migration как способ доставки индекса;
- осторожную оптимизацию после измерения;
- документирование performance evidence;
- применение GoF/GRASP без overengineering.

## Что именно проверяем

В проекте есть несколько запросов, которые будут часто выполняться в реальном backend:

1. История сделок клиента.
2. Последняя котировка по символу.
3. Проверка существования active alert для deduplication.
4. Поиск открытых позиций по символу после прихода новой котировки.

Их можно связать с реальными частями системы:

| Сценарий | Таблица | Почему hot path |
| --- | --- | --- |
| История сделок | `trades` | Частый read endpoint и аналитика клиента |
| Последняя котировка | `quotes` | Risk Engine должен брать свежую цену |
| Active alert deduplication | `risk_alerts` | Проверяется перед созданием alert-а |
| Open positions lookup | `positions` | Выполняется при quote-driven risk evaluation |

Особенно интересен четвертый сценарий. При каждой новой котировке `QuoteRiskEvaluationDispatcher` ищет открытые позиции по символу:

```csharp
Task<IReadOnlyCollection<Position>> ListOpenBySymbolAsync(
    Symbol symbol,
    CancellationToken cancellationToken);
```

EF-запрос в infrastructure layer выглядит так:

```csharp
dbContext.Positions
    .AsNoTracking()
    .Where(position => position.Symbol == symbol && position.NetVolume != 0)
    .OrderBy(position => position.TradingAccountId)
```

Flat positions (`NetVolume = 0`) не требуют quote-driven risk evaluation. Значит, если в таблице много закрытых позиций, обычный индекс по `symbol` может давать лишнее чтение.

## Паттерны GoF/GRASP в этой главе

### GRASP Information Expert

Решение о том, какой SQL-предикат нужен для поиска открытых позиций, остается около persistence/query boundary.

`Position` как доменная сущность знает, что такое `NetVolume`, но она не должна знать о PostgreSQL partial indexes. Infrastructure layer является Information Expert для вопроса "как эффективно прочитать нужные строки из конкретной БД".

### GRASP Indirection

Risk Engine не зависит от SQL и индексов напрямую.

Он вызывает `IPositionRepository.ListOpenBySymbolAsync(...)`, а EF-реализация уже использует схему БД. Это сохраняет чистую границу:

```text
QuoteRiskEvaluationDispatcher -> IPositionRepository -> EfPositionRepository -> PostgreSQL
```

Если позже появится cache, materialized view или отдельная read model, Risk Engine не придется переписывать.

### Protected Variations

Изменчивое место системы - способ поиска открытых позиций.

Стабильное место - потребность Risk Engine получить список позиций, на которые влияет новая котировка.

Repository port защищает стабильный код от изменений в PostgreSQL-плане, индексах или будущей read-model оптимизации.

### Почему не добавляем лишние паттерны

Здесь не нужен отдельный Strategy или Abstract Factory для выбора индекса.

Индекс - это инфраструктурное решение, а не доменный алгоритм. Поэтому достаточно:

- repository port;
- EF configuration;
- migration;
- воспроизводимого `EXPLAIN ANALYZE` отчета.

Это и есть отказ от overengineering: оптимизируем конкретный query path, но не строим вокруг него отдельный framework.

## Реализация шаг за шагом

### Шаг 1. Готовим seed-набор данных

Добавлен файл:

```text
docs/explain-analyze/sql/01-seed-performance-data.sql
```

Скрипт рассчитан на disposable PostgreSQL database. Он создает:

- 2 000 клиентов;
- 4 000 торговых счетов;
- 12 000 позиций;
- 240 000 сделок;
- 300 000 котировок;
- active и resolved risk alerts.

Важно, что позиции распределены не случайно "для красоты". Для проверки partial index нужно, чтобы в таблице было много flat positions и заметно меньше открытых позиций:

```sql
case
    when row_number() over (order by account_no, symbol) % 5 = 0
        then (row_number() over (order by account_no, symbol) % 11) - 5
    else 0
end::numeric as net_volume
```

Так мы моделируем типичную картину: исторических/закрытых позиций много, а активных позиций меньше.

### Шаг 2. Описываем hot-path запросы

Добавлен файл:

```text
docs/explain-analyze/sql/02-explain-hot-paths.sql
```

В нем четыре `EXPLAIN ANALYZE`:

```sql
explain (analyze, buffers, verbose, settings)
select ...
from trades
where client_id = ...
order by created_at desc
limit 100;
```

```sql
explain (analyze, buffers, verbose, settings)
select ...
from quotes
where symbol = 'EURUSD'
order by timestamp desc
limit 1;
```

```sql
explain (analyze, buffers, verbose, settings)
select exists (
    select 1
    from risk_alerts
    where ...
      and resolved_at is null
);
```

```sql
explain (analyze, buffers, verbose, settings)
select ...
from positions
where symbol = 'EURUSD'
  and net_volume <> 0
order by trading_account_id;
```

Мы включаем `buffers`, потому что для PostgreSQL важно видеть не только время, но и чтение страниц. На маленьком локальном dataset timings могут быть нестабильны, а `Buffers` помогает понять форму работы плана.

### Шаг 3. Поднимаем disposable PostgreSQL

В идеале эти скрипты удобно запускать через Docker или Testcontainers. В локальной среде Docker Engine оказался недоступен:

```text
dockerDesktopLinuxEngine pipe missing
```

Также существующая локальная PostgreSQL-служба не приняла проектные credentials `pulserisk/pulserisk`.

Чтобы не выдумывать результаты, был поднят временный PostgreSQL 18 cluster из локально установленных binaries на порту `55432`. Он использовался только для:

1. применения EF Core migrations;
2. запуска seed script;
3. снятия `EXPLAIN ANALYZE`;
4. удаления временных данных после проверки.

Это важная дисциплина: если измерение не получилось, нельзя подменять его "ожидаемыми" цифрами. Нужно либо зафиксировать блокер, либо создать воспроизводимое окружение.

### Шаг 4. Смотрим первые результаты

После применения migrations и seed script были сняты планы:

```text
docs/explain-analyze/reports/2026-07-02-explain-output.txt
```

Результаты:

| Запрос | Индекс | Execution Time |
| --- | --- | --- |
| История сделок клиента | `ix_trades_client_id_created_at` | `0.293 ms` |
| Последняя котировка | `ix_quotes_symbol_timestamp` | `0.039 ms` |
| Active alert deduplication | `ix_risk_alerts_client_id_trading_account_id_symbol_alert_type` | `0.025 ms` |
| Open positions lookup | `ix_positions_symbol_open_trading_account` | `0.772 ms` |

Первые три запроса уже используют ожидаемые индексы.

Для open positions lookup нужен отдельный before/after, потому что именно туда добавляется новый partial index.

### Шаг 5. Добавляем partial index для открытых позиций

В `PositionConfiguration` добавлена EF Core конфигурация:

```csharp
builder.HasIndex(position => new { position.Symbol, position.TradingAccountId })
    .HasDatabaseName("ix_positions_symbol_open_trading_account")
    .HasFilter("net_volume <> 0");
```

Смысл индекса:

```sql
create index ix_positions_symbol_open_trading_account
on positions (symbol, trading_account_id)
where net_volume <> 0;
```

Почему именно так:

- `symbol` - первый столбец, потому что запрос фильтрует по инструменту;
- `trading_account_id` - второй столбец, потому что запрос сортирует результат по счету;
- `where net_volume <> 0` - partial predicate, потому что flat positions не нужны для quote-driven risk evaluation.

Это не замена unique index `positions(trading_account_id, symbol)`. Unique index отвечает за инвариант "на один счет и символ есть одна позиция". Новый partial index отвечает за другой вопрос: "быстро найти все открытые позиции по символу".

### Шаг 6. Создаем EF Core migration

Сгенерирована миграция:

```text
src/PulseRisk.Infrastructure/Persistence/Migrations/20260702124746_AddOpenPositionSymbolIndex.cs
```

В ней только одно изменение:

```csharp
migrationBuilder.CreateIndex(
    name: "ix_positions_symbol_open_trading_account",
    table: "positions",
    columns: new[] { "symbol", "trading_account_id" },
    filter: "net_volume <> 0");
```

Миграция узкая. Она не трогает таблицы, данные или unrelated indexes.

### Шаг 7. Снимаем before/after отчет

Добавлен файл:

```text
docs/explain-analyze/sql/03-open-positions-before-after.sql
```

Он намеренно:

1. удаляет `ix_positions_symbol_open_trading_account`;
2. снимает план open positions lookup;
3. создает индекс заново;
4. снимает план еще раз.

Отчет сохранен в:

```text
docs/explain-analyze/reports/2026-07-02-open-positions-before-after.txt
```

До индекса PostgreSQL использовал обычный индекс по `symbol`:

```text
Bitmap Index Scan on ix_positions_symbol
rows=4000
Rows Removed by Filter: 3273
Execution Time: 0.668 ms
```

После индекса PostgreSQL использовал partial index:

```text
Bitmap Index Scan on ix_positions_symbol_open_trading_account
rows=727
Execution Time: 0.340 ms
```

Вывод: индекс решает конкретную проблему. Он уменьшает лишнее чтение flat positions при quote-driven перерасчете риска.

### Шаг 8. Обновляем документацию

Обновлены:

- `docs/explain-analyze/README.md`;
- `docs/explain-analyze/reports/2026-07-02-local-status.md`;
- `ARCHITECTURE.md`;
- `README.md`;
- `DEVELOPMENT_PLAN.md`;
- `book/progress.md`.

В `ARCHITECTURE.md` индекс описан не как механическая строка в списке, а как решение конкретного hot path:

```text
QuoteRiskEvaluationDispatcher
-> IPositionRepository.ListOpenBySymbolAsync(...)
-> positions(symbol, trading_account_id) where net_volume <> 0
```

## Как читать EXPLAIN ANALYZE в этой главе

Для текущей итерации достаточно смотреть на несколько вещей.

### Index Scan / Bitmap Index Scan

Нужно убедиться, что PostgreSQL использует ожидаемый индекс.

Для latest quote:

```text
Index Scan using ix_quotes_symbol_timestamp
```

Для active alert:

```text
Index Only Scan using ix_risk_alerts_client_id_trading_account_id_symbol_alert_type
```

Для open positions после оптимизации:

```text
Bitmap Index Scan on ix_positions_symbol_open_trading_account
```

### Rows Removed by Filter

Это особенно полезно для поиска лишнего чтения.

До partial index:

```text
Rows Removed by Filter: 3273
```

PostgreSQL нашел 4 000 позиций по `EURUSD`, но 3 273 из них оказались flat positions.

После partial index этой строки уже нет, потому что индекс содержит только `net_volume <> 0`.

### Execution Time

Execution Time полезен, но на локальной машине его нельзя абсолютизировать.

На маленьком dataset разница `0.668 ms` и `0.340 ms` показывает направление эффекта, но не является production SLA. Важнее, что форма плана стала лучше: меньше лишних строк, меньше фильтрации, индекс соответствует предикату запроса.

### Sort

В after-плане остался `Sort`.

Это не ошибка. Planner выбрал `Bitmap Heap Scan`, а не обычный ordered index scan. Для текущего объема это нормально. Если в load test сортировка станет bottleneck, можно отдельно исследовать:

- изменение статистики;
- `enable_bitmapscan` для диагностического сравнения;
- покрывающий индекс;
- read model;
- cache/sampling.

Но сейчас это было бы преждевременной оптимизацией.

## Почему не оптимизируем все сразу

В проекте можно придумать много оптимизаций:

- latest quote cache;
- materialized read model для открытых позиций;
- partitioning quotes/trades по времени;
- bulk insert через `COPY`;
- Dapper query object для всех read paths;
- compiled EF queries.

Но в этой главе мы делаем только один production-похожий шаг:

```text
измерили -> увидели лишнее чтение -> добавили точечный индекс -> подтвердили планом
```

Такой подход лучше демонстрирует инженерную зрелость, чем список "возможных ускорений" без доказательств.

## Проверка результата

Проверки для этой главы:

```bash
dotnet test PulseRisk.slnx
```

Результат текущей итерации:

- unit-тесты: `69/69`;
- integration tests: `6/9`;
- skipped integration tests: `3/9`.

Три skipped tests зависят от Docker/Testcontainers PostgreSQL. В локальном окружении Docker Engine недоступен, поэтому skip является ожидаемым поведением. Для performance evidence этой главы использовался отдельный disposable PostgreSQL 18 cluster.

```bash
git diff --check
```

Также вручную проверяются документы:

- `docs/explain-analyze/reports/2026-07-02-explain-output.txt`;
- `docs/explain-analyze/reports/2026-07-02-open-positions-before-after.txt`;
- `docs/explain-analyze/reports/2026-07-02-local-status.md`.

## Что изменилось в репозитории

- Добавлен partial index `ix_positions_symbol_open_trading_account`.
- Добавлена EF Core migration `AddOpenPositionSymbolIndex`.
- Добавлены SQL-скрипты для performance seed и `EXPLAIN ANALYZE`.
- Сохранены локальные PostgreSQL reports.
- Обновлены `README.md`, `ARCHITECTURE.md`, `DEVELOPMENT_PLAN.md`, `book/progress.md`.
- Добавлена эта глава книги.

## Альтернативы

- **Оставить только `ix_positions_symbol`**: проще, но запрос читает все позиции по символу и фильтрует flat positions.
- **Сделать индекс `(symbol, net_volume, trading_account_id)`**: возможный вариант, но `net_volume <> 0` лучше выражается partial index, потому что точное значение объема не ищется.
- **Сделать read model открытых позиций**: быстрее для очень больших объемов, но добавляет синхронизацию и eventual consistency до появления доказанной необходимости.
- **Добавить latest quote cache в этой же главе**: полезно позже, но смешало бы SQL-оптимизацию и cache-инвалидацию.
- **Переписать запрос на Dapper**: не решает проблему лишних строк само по себе; сначала нужен правильный индекс.

## Частые ошибки

- Добавлять индексы без привязки к запросу.
- Ориентироваться только на Execution Time и игнорировать форму плана.
- Снимать `EXPLAIN ANALYZE` на пустой базе.
- Забывать `ANALYZE` после большого seed script.
- Делать performance claims без сохраненного отчета.
- Запускать seed script на базе с ценными данными.
- Добавлять cache до проверки, что bottleneck действительно в БД.

## Вопросы для интервью

- Почему `positions(symbol, trading_account_id) WHERE net_volume <> 0` лучше обычного индекса по `symbol` для quote-driven lookup?
- Почему unique index `positions(trading_account_id, symbol)` не закрывает этот hot path?
- Что означает `Rows Removed by Filter` в плане до индекса?
- Почему after-план все еще может содержать `Sort`?
- Почему timings из локального `EXPLAIN ANALYZE` нельзя считать SLA?
- Когда partial index перестанет быть достаточным и понадобится read model/cache?
- Как бы вы проверили эффект индекса на большем dataset?

## Чек-лист главы

- [x] Seed script для performance dataset добавлен.
- [x] Hot-path `EXPLAIN ANALYZE` script добавлен.
- [x] Before/after script для open positions lookup добавлен.
- [x] Disposable PostgreSQL окружение использовано для реальных планов.
- [x] Partial index для open positions lookup добавлен.
- [x] EF Core migration создана.
- [x] EXPLAIN reports сохранены.
- [x] Архитектурное обоснование индекса описано.
- [x] План и прогресс книги обновлены.
- [x] Проверки выполнены.
