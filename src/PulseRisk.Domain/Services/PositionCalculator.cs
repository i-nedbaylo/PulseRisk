using PulseRisk.Domain.Enums;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Domain.Services;

public static class PositionCalculator
{
    public static PositionCalculationResult ApplyTrade(
        PositionCalculationState current,
        TradeSide side,
        Volume volume,
        Price price)
    {
        var delta = side == TradeSide.Buy ? volume.Value : -volume.Value;
        var currentNetVolume = current.NetVolume;
        var newNetVolume = currentNetVolume + delta;

        if (newNetVolume == 0)
        {
            return new PositionCalculationResult(0, 0);
        }

        if (currentNetVolume == 0 || Math.Sign(currentNetVolume) != Math.Sign(delta))
        {
            return ApplyReducingOrFlippingTrade(current, delta, newNetVolume, price);
        }

        var currentAbsVolume = Math.Abs(currentNetVolume);
        var deltaAbsVolume = Math.Abs(delta);
        var newAbsVolume = Math.Abs(newNetVolume);
        var weightedAveragePrice =
            ((currentAbsVolume * current.AveragePrice) + (deltaAbsVolume * price.Value)) / newAbsVolume;

        return new PositionCalculationResult(newNetVolume, weightedAveragePrice);
    }

    private static PositionCalculationResult ApplyReducingOrFlippingTrade(
        PositionCalculationState current,
        decimal delta,
        decimal newNetVolume,
        Price price)
    {
        if (current.NetVolume == 0)
        {
            return new PositionCalculationResult(newNetVolume, price.Value);
        }

        if (Math.Sign(current.NetVolume) == Math.Sign(newNetVolume))
        {
            return new PositionCalculationResult(newNetVolume, current.AveragePrice);
        }

        return new PositionCalculationResult(newNetVolume, price.Value);
    }
}

