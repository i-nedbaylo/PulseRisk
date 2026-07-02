-- PulseRisk hot-path query plans.
-- Run after sql/01-seed-performance-data.sql.

\echo '1. Client trade history'
explain (analyze, buffers, verbose, settings)
select
    id,
    client_id,
    trading_account_id,
    symbol,
    side,
    volume,
    open_price,
    created_at
from trades
where client_id = '11111111-0000-0000-0000-000000000001'::uuid
order by created_at desc
limit 100;

\echo '2. Latest quote by symbol'
explain (analyze, buffers, verbose, settings)
select
    id,
    symbol,
    bid,
    ask,
    timestamp
from quotes
where symbol = 'EURUSD'
order by timestamp desc
limit 1;

\echo '3. Active alert deduplication lookup'
explain (analyze, buffers, verbose, settings)
select exists (
    select 1
    from risk_alerts
    where client_id = '11111111-0000-0000-0000-000000000001'::uuid
      and trading_account_id = '22222222-0000-0000-0000-000000000001'::uuid
      and symbol = 'EURUSD'
      and alert_type = 'ExposureLimitExceeded'
      and resolved_at is null
);

\echo '4. Quote-driven open positions lookup'
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

