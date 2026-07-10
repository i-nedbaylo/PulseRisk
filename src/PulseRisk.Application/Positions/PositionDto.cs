namespace PulseRisk.Application.Positions;

public sealed record PositionDto(
    Guid Id,
    Guid ClientId,
    Guid TradingAccountId,
    string Symbol,
    decimal NetVolume,
    decimal AveragePrice,
    decimal FloatingPnL,
    DateTimeOffset UpdatedAt);
