namespace PulseRisk.Benchmarks.LatencySampling;

internal sealed record DomainLatencySample(
    int Iterations,
    double PositionCalculationP95Nanoseconds,
    double PnLCalculationP95Nanoseconds);
