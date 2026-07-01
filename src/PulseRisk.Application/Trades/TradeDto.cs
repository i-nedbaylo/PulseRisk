using PulseRisk.Domain.Enums;

namespace PulseRisk.Application.Trades;

public sealed record TradeDto(
    Guid Id,
    Guid ClientId,
    Guid TradingAccountId,
    string Symbol,
    TradeSide Side,
    decimal Volume,
    decimal OpenPrice,
    DateTimeOffset CreatedAt);
