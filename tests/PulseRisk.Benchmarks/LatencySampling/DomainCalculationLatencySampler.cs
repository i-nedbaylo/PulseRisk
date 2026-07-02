using System.Diagnostics;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.Services;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Benchmarks.LatencySampling;

internal static class DomainCalculationLatencySampler
{
    public static DomainLatencySample Run(int iterations)
    {
        var positionLatencies = new double[iterations];
        var pnlLatencies = new double[iterations];
        var state = new PositionCalculationState(10m, 1.1000m);
        var volume = new Volume(10m);
        var price = new Price(1.2000m);
        var bid = new Price(1.2510m);
        var ask = new Price(1.2512m);

        for (var i = 0; i < iterations; i++)
        {
            var startedAt = Stopwatch.GetTimestamp();
            _ = PositionCalculator.ApplyTrade(state, TradeSide.Buy, volume, price);
            positionLatencies[i] = Stopwatch.GetElapsedTime(startedAt).TotalNanoseconds;

            startedAt = Stopwatch.GetTimestamp();
            _ = PnLCalculator.CalculateFloatingPnL(2.5m, 1.2400m, bid, ask, 100_000m);
            pnlLatencies[i] = Stopwatch.GetElapsedTime(startedAt).TotalNanoseconds;
        }

        return new DomainLatencySample(
            iterations,
            CalculateP95(positionLatencies),
            CalculateP95(pnlLatencies));
    }

    private static double CalculateP95(double[] values)
    {
        Array.Sort(values);
        var index = Math.Clamp(
            (int)Math.Ceiling(values.Length * 0.95d) - 1,
            0,
            values.Length - 1);

        return values[index];
    }
}
