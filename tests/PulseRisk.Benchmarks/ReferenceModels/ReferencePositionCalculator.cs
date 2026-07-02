using PulseRisk.Domain.Enums;

namespace PulseRisk.Benchmarks.ReferenceModels;

public static class ReferencePositionCalculator
{
    public static ReferencePositionCalculationResult ApplyTrade(
        ReferencePositionCalculationState current,
        TradeSide side,
        decimal volume,
        decimal price)
    {
        var delta = side == TradeSide.Buy ? volume : -volume;
        var currentNetVolume = current.NetVolume;
        var newNetVolume = currentNetVolume + delta;

        if (newNetVolume == 0)
        {
            return new ReferencePositionCalculationResult(0, 0);
        }

        if (currentNetVolume == 0 || Math.Sign(currentNetVolume) != Math.Sign(delta))
        {
            return ApplyReducingOrFlippingTrade(current, delta, newNetVolume, price);
        }

        var currentAbsVolume = Math.Abs(currentNetVolume);
        var deltaAbsVolume = Math.Abs(delta);
        var newAbsVolume = Math.Abs(newNetVolume);
        var weightedAveragePrice =
            ((currentAbsVolume * current.AveragePrice) + (deltaAbsVolume * price)) / newAbsVolume;

        return new ReferencePositionCalculationResult(newNetVolume, weightedAveragePrice);
    }

    private static ReferencePositionCalculationResult ApplyReducingOrFlippingTrade(
        ReferencePositionCalculationState current,
        decimal delta,
        decimal newNetVolume,
        decimal price)
    {
        if (current.NetVolume == 0)
        {
            return new ReferencePositionCalculationResult(newNetVolume, price);
        }

        if (Math.Sign(current.NetVolume) == Math.Sign(newNetVolume))
        {
            return new ReferencePositionCalculationResult(newNetVolume, current.AveragePrice);
        }

        return new ReferencePositionCalculationResult(newNetVolume, price);
    }
}
