using BenchmarkDotNet.Attributes;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Services;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class PnLCalculationBenchmarks
{
    private readonly Symbol _symbol = new("EURUSD");
    private readonly Price _bid = new(1.2510m);
    private readonly Price _ask = new(1.2512m);
    private readonly PositionCalculationState _position = new(2.5m, 1.2400m);
    private Quote _quote = null!;

    [GlobalSetup]
    public void Setup()
    {
        _quote = new Quote(Guid.NewGuid(), _symbol, _bid, _ask, DateTimeOffset.UtcNow);
    }

    [Benchmark]
    public decimal ObjectHeavyReferencePath()
    {
        var position = new PositionCalculationState(2.5m, 1.2400m);
        var quote = new Quote(Guid.NewGuid(), _symbol, _bid, _ask, DateTimeOffset.UtcNow);

        return PnLCalculator.CalculateFloatingPnL(position, quote, contractSize: 100_000m);
    }

    [Benchmark]
    public decimal ProductionEntityPath()
    {
        return PnLCalculator.CalculateFloatingPnL(_position, _quote, contractSize: 100_000m);
    }

    [Benchmark]
    public decimal ProductionPrimitivePath()
    {
        return PnLCalculator.CalculateFloatingPnL(
            netVolume: 2.5m,
            averagePrice: 1.2400m,
            _bid,
            _ask,
            contractSize: 100_000m);
    }
}
