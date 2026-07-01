using PulseRisk.Domain.Enums;

namespace PulseRisk.Application.Events;

public sealed record TradeAcceptedEvent(
    Guid TradeId,
    Guid ClientId,
    Guid TradingAccountId,
    string Symbol,
    TradeSide Side,
    decimal Volume,
    decimal OpenPrice,
    DateTimeOffset OccurredAt);
