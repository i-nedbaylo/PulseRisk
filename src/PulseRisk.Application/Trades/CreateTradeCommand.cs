using PulseRisk.Domain.Enums;

namespace PulseRisk.Application.Trades;

public sealed record CreateTradeCommand(
    Guid ClientId,
    Guid TradingAccountId,
    string Symbol,
    TradeSide Side,
    decimal Volume,
    decimal OpenPrice);
