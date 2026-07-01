using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Domain.Risk;

public sealed record RiskMetricSnapshot(
    Guid ClientId,
    Guid TradingAccountId,
    Symbol Symbol,
    decimal NetExposure,
    decimal FloatingPnL,
    decimal Equity,
    decimal MarginUsed,
    decimal MarginLevel);

