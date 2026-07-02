# PostgreSQL EXPLAIN ANALYZE

Этот каталог хранит воспроизводимые артефакты для проверки hot-path запросов PulseRisk.

## Что проверяем

- история сделок клиента: `trades(client_id, created_at DESC)`;
- последняя котировка по символу: `quotes(symbol, timestamp DESC)`;
- active-alert deduplication: partial index `risk_alerts(client_id, trading_account_id, symbol, alert_type) WHERE resolved_at IS NULL`;
- quote-driven open positions lookup: partial index `positions(symbol, trading_account_id) WHERE net_volume <> 0`.

## Как запустить

1. Поднять PostgreSQL и применить EF migrations.
2. Выполнить `sql/01-seed-performance-data.sql` на disposable базе.
3. Выполнить `sql/02-explain-hot-paths.sql`.
4. При необходимости выполнить `sql/03-open-positions-before-after.sql`, чтобы сравнить план до и после partial index.
5. Сохранить вывод в `reports/`.

Пример через `psql`:

```bash
psql "$PULSERISK_EXPLAIN_CONNECTION" -f docs/explain-analyze/sql/01-seed-performance-data.sql
psql "$PULSERISK_EXPLAIN_CONNECTION" -f docs/explain-analyze/sql/02-explain-hot-paths.sql > docs/explain-analyze/reports/YYYY-MM-DD-explain-output.txt
psql "$PULSERISK_EXPLAIN_CONNECTION" -f docs/explain-analyze/sql/03-open-positions-before-after.sql > docs/explain-analyze/reports/YYYY-MM-DD-open-positions-before-after.txt
```

Скрипты рассчитаны на отдельную performance/test базу. Не запускайте seed script на базе с ценными данными.

## Локальные результаты 2026-07-02

В локальном disposable PostgreSQL 18 cluster были сняты:

- `reports/2026-07-02-explain-output.txt`;
- `reports/2026-07-02-open-positions-before-after.txt`;
- `reports/2026-07-02-local-status.md`.

Ключевой вывод: quote-driven lookup открытых позиций получил точечный partial index `ix_positions_symbol_open_trading_account`. В тестовом наборе запрос по `EURUSD` до индекса читал 4 000 позиций по символу и отбрасывал 3 273 flat positions фильтром; после индекса сразу читались 727 открытых позиций.
