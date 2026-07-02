-- Compare quote-driven open positions lookup before and after the partial index.
-- Run only on a disposable PostgreSQL database after EF migrations and seed data.
-- The script intentionally drops and recreates the index to capture both plans.

\echo 'Before ix_positions_symbol_open_trading_account'
drop index if exists ix_positions_symbol_open_trading_account;
analyze positions;

explain (analyze, buffers, verbose, settings)
select
    id,
    client_id,
    trading_account_id,
    symbol,
    net_volume,
    average_price,
    floating_pn_l,
    updated_at
from positions
where symbol = 'EURUSD'
  and net_volume <> 0
order by trading_account_id;

\echo 'After ix_positions_symbol_open_trading_account'
create index ix_positions_symbol_open_trading_account
on positions (symbol, trading_account_id)
where net_volume <> 0;
analyze positions;

explain (analyze, buffers, verbose, settings)
select
    id,
    client_id,
    trading_account_id,
    symbol,
    net_volume,
    average_price,
    floating_pn_l,
    updated_at
from positions
where symbol = 'EURUSD'
  and net_volume <> 0
order by trading_account_id;
