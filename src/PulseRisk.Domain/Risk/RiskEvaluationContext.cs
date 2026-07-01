using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Domain.Risk;

public sealed record RiskEvaluationContext(
    Guid ClientId,
    Guid TradingAccountId,
    Symbol Symbol,
    decimal NetExposure,
    decimal FloatingPnL,
    decimal MarginLevel);

