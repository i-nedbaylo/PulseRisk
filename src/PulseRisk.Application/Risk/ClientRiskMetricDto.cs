namespace PulseRisk.Application.Risk;

public sealed record ClientRiskMetricDto(
    Guid ClientId,
    Guid TradingAccountId,
    string Symbol,
    decimal NetVolume,
    decimal AveragePrice,
    decimal NetExposure,
    decimal FloatingPnL,
    decimal Equity,
    decimal MarginUsed,
    decimal MarginLevel,
    DateTimeOffset? LatestQuoteTimestamp);
