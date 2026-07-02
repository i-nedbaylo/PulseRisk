using BenchmarkDotNet.Attributes;
using PulseRisk.Benchmarks.ReferenceModels;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.Services;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class PositionCalculationBenchmarks
{
    private readonly ReferencePositionCalculationState _referenceLong = new(10m, 1.1000m);
    private readonly PositionCalculationState _productionLong = new(10m, 1.1000m);
    private readonly Volume _volume = new(10m);
    private readonly Price _price = new(1.2000m);

    [Benchmark]
    public ReferencePositionCalculationResult ReferenceRecordResult()
    {
        return ReferencePositionCalculator.ApplyTrade(
            _referenceLong,
            TradeSide.Buy,
            volume: 10m,
            price: 1.2000m);
    }

    [Benchmark]
    public PositionCalculationResult ProductionValueResult()
    {
        return PositionCalculator.ApplyTrade(
            _productionLong,
            TradeSide.Buy,
            _volume,
            _price);
    }
}
