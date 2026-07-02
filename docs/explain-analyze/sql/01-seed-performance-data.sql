-- PulseRisk performance dataset.
-- Run only on a disposable PostgreSQL database after EF migrations.

begin;

insert into clients (id, name, status, created_at)
select
    ('11111111-0000-0000-0000-' || lpad(gs::text, 12, '0'))::uuid,
    'Performance Client ' || gs,
    'Active',
    timestamp with time zone '2026-07-01 00:00:00+00' + (gs * interval '1 minute')
from generate_series(1, 2000) as gs
on conflict (id) do nothing;

with accounts as (
    select
        gs as account_no,
        ((gs - 1) / 2 + 1)::int as client_no
    from generate_series(1, 4000) as gs
)
insert into trading_accounts (id, client_id, balance, currency, leverage, created_at)
select
    ('22222222-0000-0000-0000-' || lpad(account_no::text, 12, '0'))::uuid,
    ('11111111-0000-0000-0000-' || lpad(client_no::text, 12, '0'))::uuid,
    100000 + account_no,
    'USD',
    100,
    timestamp with time zone '2026-07-01 00:00:00+00' + (account_no * interval '30 seconds')
from accounts
on conflict (id) do nothing;

with account_symbols as (
    select
        a.account_no,
        a.client_no,
        s.symbol,
        s.base_price
    from (
        select
            gs as account_no,
            ((gs - 1) / 2 + 1)::int as client_no
        from generate_series(1, 4000) as gs
    ) as a
    cross join (
        values
            ('EURUSD', 1.25000000::numeric),
            ('GBPUSD', 1.30000000::numeric),
            ('XAUUSD', 2300.00000000::numeric)
    ) as s(symbol, base_price)
),
numbered_positions as (
    select
        row_number() over (order by account_no, symbol) as position_no,
        account_no,
        client_no,
        symbol,
        base_price,
        case
            when row_number() over (order by account_no, symbol) % 5 = 0 then (row_number() over (order by account_no, symbol) % 11) - 5
            else 0
        end::numeric as net_volume
    from account_symbols
)
insert into positions (id, client_id, trading_account_id, symbol, net_volume, average_price, floating_pn_l, updated_at)
select
    ('33333333-0000-0000-0000-' || lpad(position_no::text, 12, '0'))::uuid,
    ('11111111-0000-0000-0000-' || lpad(client_no::text, 12, '0'))::uuid,
    ('22222222-0000-0000-0000-' || lpad(account_no::text, 12, '0'))::uuid,
    symbol,
    net_volume,
    case when net_volume = 0 then 0 else base_price end,
    0,
    timestamp with time zone '2026-07-02 00:00:00+00'
from numbered_positions
on conflict (id) do nothing;

with trade_rows as (
    select
        gs as trade_no,
        ((gs - 1) % 4000 + 1)::int as account_no,
        ((((gs - 1) % 4000 + 1) - 1) / 2 + 1)::int as client_no,
        (array['EURUSD', 'GBPUSD', 'XAUUSD'])[((gs - 1) % 3) + 1] as symbol
    from generate_series(1, 240000) as gs
)
insert into trades (id, client_id, trading_account_id, symbol, side, volume, open_price, created_at)
select
    ('44444444-0000-0000-0000-' || lpad(trade_no::text, 12, '0'))::uuid,
    ('11111111-0000-0000-0000-' || lpad(client_no::text, 12, '0'))::uuid,
    ('22222222-0000-0000-0000-' || lpad(account_no::text, 12, '0'))::uuid,
    symbol,
    case when trade_no % 2 = 0 then 'Buy' else 'Sell' end,
    ((trade_no % 10) + 1)::numeric / 10,
    case symbol
        when 'EURUSD' then 1.20 + ((trade_no % 500)::numeric / 100000)
        when 'GBPUSD' then 1.28 + ((trade_no % 500)::numeric / 100000)
        else 2250 + (trade_no % 500)
    end,
    timestamp with time zone '2026-07-01 00:00:00+00' + (trade_no * interval '1 second')
from trade_rows
on conflict (id) do nothing;

with quote_rows as (
    select
        gs as quote_no,
        (array['EURUSD', 'GBPUSD', 'XAUUSD'])[((gs - 1) % 3) + 1] as symbol
    from generate_series(1, 300000) as gs
)
insert into quotes (id, symbol, bid, ask, timestamp)
select
    ('55555555-0000-0000-0000-' || lpad(quote_no::text, 12, '0'))::uuid,
    symbol,
    case symbol
        when 'EURUSD' then 1.25 + ((quote_no % 1000)::numeric / 1000000)
        when 'GBPUSD' then 1.30 + ((quote_no % 1000)::numeric / 1000000)
        else 2300 + ((quote_no % 1000)::numeric / 10)
    end,
    case symbol
        when 'EURUSD' then 1.2501 + ((quote_no % 1000)::numeric / 1000000)
        when 'GBPUSD' then 1.3001 + ((quote_no % 1000)::numeric / 1000000)
        else 2300.1 + ((quote_no % 1000)::numeric / 10)
    end,
    timestamp with time zone '2026-07-01 00:00:00+00' + (quote_no * interval '100 milliseconds')
from quote_rows
on conflict (id) do nothing;

with open_positions as (
    select
        row_number() over (order by trading_account_id, symbol) as alert_no,
        client_id,
        trading_account_id,
        symbol
    from positions
    where net_volume <> 0
    limit 12000
)
insert into risk_alerts (id, client_id, trading_account_id, symbol, alert_type, severity, message, created_at, resolved_at)
select
    ('66666666-0000-0000-0000-' || lpad(alert_no::text, 12, '0'))::uuid,
    client_id,
    trading_account_id,
    symbol,
    'ExposureLimitExceeded',
    'Critical',
    'Performance active exposure alert',
    timestamp with time zone '2026-07-02 00:00:00+00' + (alert_no * interval '1 second'),
    null
from open_positions
on conflict (id) do nothing;

with resolved_alert_rows as (
    select
        gs as alert_no,
        ((gs - 1) % 4000 + 1)::int as account_no,
        ((((gs - 1) % 4000 + 1) - 1) / 2 + 1)::int as client_no,
        (array['EURUSD', 'GBPUSD', 'XAUUSD'])[((gs - 1) % 3) + 1] as symbol
    from generate_series(1, 60000) as gs
)
insert into risk_alerts (id, client_id, trading_account_id, symbol, alert_type, severity, message, created_at, resolved_at)
select
    ('77777777-0000-0000-0000-' || lpad(alert_no::text, 12, '0'))::uuid,
    ('11111111-0000-0000-0000-' || lpad(client_no::text, 12, '0'))::uuid,
    ('22222222-0000-0000-0000-' || lpad(account_no::text, 12, '0'))::uuid,
    symbol,
    case when alert_no % 3 = 0 then 'MarginLevelWarning' else 'ExposureLimitExceeded' end,
    case when alert_no % 3 = 0 then 'Warning' else 'Critical' end,
    'Performance resolved alert',
    timestamp with time zone '2026-07-01 00:00:00+00' + (alert_no * interval '2 seconds'),
    timestamp with time zone '2026-07-01 00:00:00+00' + (alert_no * interval '2 seconds') + interval '15 minutes'
from resolved_alert_rows
on conflict (id) do nothing;

analyze clients;
analyze trading_accounts;
analyze instruments;
analyze positions;
analyze trades;
analyze quotes;
analyze risk_alerts;

commit;
