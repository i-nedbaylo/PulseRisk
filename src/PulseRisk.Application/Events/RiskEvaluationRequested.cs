namespace PulseRisk.Application.Events;

public sealed record RiskEvaluationRequested(
    Guid ClientId,
    Guid TradingAccountId,
    string Symbol,
    Guid? TradeId,
    Guid? PositionId,
    string Reason,
    DateTimeOffset RequestedAt);
