namespace PulseRisk.Application.Events;

public sealed record PositionChangedEvent(
    Guid TradeId,
    Guid PositionId,
    Guid ClientId,
    Guid TradingAccountId,
    string Symbol,
    decimal NetVolume,
    decimal AveragePrice,
    decimal FloatingPnL,
    DateTimeOffset OccurredAt);
