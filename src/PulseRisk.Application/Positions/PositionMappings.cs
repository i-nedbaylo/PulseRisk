using PulseRisk.Domain.Entities;

namespace PulseRisk.Application.Positions;

internal static class PositionMappings
{
    public static PositionDto ToDto(this Position position)
    {
        return new PositionDto(
            position.Id,
            position.ClientId,
            position.TradingAccountId,
            position.Symbol.Value,
            position.NetVolume,
            position.AveragePrice,
            position.FloatingPnL,
            position.UpdatedAt);
    }
}
