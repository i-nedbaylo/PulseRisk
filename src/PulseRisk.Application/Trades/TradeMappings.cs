using PulseRisk.Domain.Entities;

namespace PulseRisk.Application.Trades;

internal static class TradeMappings
{
    public static TradeDto ToDto(this Trade trade)
    {
        return new TradeDto(
            trade.Id,
            trade.ClientId,
            trade.TradingAccountId,
            trade.Symbol.Value,
            trade.Side,
            trade.Volume.Value,
            trade.OpenPrice.Value,
            trade.CreatedAt);
    }
}
