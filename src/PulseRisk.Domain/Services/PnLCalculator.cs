using PulseRisk.Domain.Entities;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Domain.Services;

public static class PnLCalculator
{
    public static decimal CalculateFloatingPnL(
        PositionCalculationState position,
        Quote quote,
        decimal contractSize)
    {
        return CalculateFloatingPnL(
            position.NetVolume,
            position.AveragePrice,
            quote.Bid,
            quote.Ask,
            contractSize);
    }

    public static decimal CalculateFloatingPnL(
        decimal netVolume,
        decimal averagePrice,
        Price bid,
        Price ask,
        decimal contractSize)
    {
        if (contractSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(contractSize), "Contract size must be greater than zero.");
        }

        if (netVolume == 0)
        {
            return 0;
        }

        if (averagePrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(averagePrice), "Average price must be greater than zero for an open position.");
        }

        if (netVolume > 0)
        {
            return (bid.Value - averagePrice) * netVolume * contractSize;
        }

        return (averagePrice - ask.Value) * Math.Abs(netVolume) * contractSize;
    }
}

