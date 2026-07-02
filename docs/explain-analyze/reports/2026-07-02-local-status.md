# 2026-07-02: локальный прогон EXPLAIN ANALYZE

## Окружение

- Host OS: Windows.
- Docker CLI: установлен.
- Docker Engine: недоступен, pipe `dockerDesktopLinuxEngine` отсутствует.
- Локальная служба PostgreSQL 18: обнаружена на `localhost:5432`.
- Проектная строка подключения: `Host=localhost;Port=5432;Database=pulserisk;Username=pulserisk;Password=pulserisk`.
- Результат подключения к локальной службе: password authentication failed for user `pulserisk`.

Чтобы не подменять результат предположениями, для главы был поднят disposable PostgreSQL 18 cluster из локально установленных PostgreSQL binaries на порту `55432`. Кластер использовался только для применения EF Core migrations, seed-скрипта и снятия `EXPLAIN ANALYZE`.

## Набор данных

Seed script `docs/explain-analyze/sql/01-seed-performance-data.sql` создал:

- `clients`: 2 000 строк;
- `trading_accounts`: 4 000 строк;
- `positions`: 12 000 строк;
- `trades`: 240 000 строк;
- `quotes`: 300 000 строк;
- active `risk_alerts`: 2 181 строка;
- resolved `risk_alerts`: 60 000 строк.

## Снятые отчеты

- `docs/explain-analyze/reports/2026-07-02-explain-output.txt` - hot-path запросы после применения миграций.
- `docs/explain-analyze/reports/2026-07-02-open-positions-before-after.txt` - сравнение lookup открытых позиций до и после partial index.

## Результаты hot-path запросов

| Запрос | Использованный индекс | Execution Time |
| --- | --- | --- |
| История сделок клиента | `ix_trades_client_id_created_at` | `0.293 ms` |
| Последняя котировка по символу | `ix_quotes_symbol_timestamp` | `0.039 ms` |
| Deduplication active alert | `ix_risk_alerts_client_id_trading_account_id_symbol_alert_type` | `0.025 ms` |
| Открытые позиции по символу | `ix_positions_symbol_open_trading_account` | `0.772 ms` |

## Эффект нового индекса

Для запроса:

```sql
select *
from positions
where symbol = 'EURUSD'
  and net_volume <> 0
order by trading_account_id;
```

без `ix_positions_symbol_open_trading_account` PostgreSQL использовал обычный индекс `ix_positions_symbol`, читал 4 000 строк по символу и отбрасывал 3 273 flat positions фильтром. Execution Time: `0.668 ms`.

После добавления partial index:

```sql
create index ix_positions_symbol_open_trading_account
on positions (symbol, trading_account_id)
where net_volume <> 0;
```

план использовал `ix_positions_symbol_open_trading_account` и сразу читал только 727 открытых позиций. Execution Time: `0.340 ms`.

Цифры относятся к локальному disposable-набору данных и не являются обещанием production latency. Их назначение - показать воспроизводимый способ проверки гипотезы и факт, что индекс решает конкретную проблему hot path.
