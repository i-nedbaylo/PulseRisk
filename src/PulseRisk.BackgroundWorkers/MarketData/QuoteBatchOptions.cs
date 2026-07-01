namespace PulseRisk.BackgroundWorkers.MarketData;

public sealed class QuoteBatchOptions
{
    public const string SectionName = "QuoteBatch";

    public int BatchSize { get; init; } = 500;

    public int FlushIntervalMilliseconds { get; init; } = 1000;
}
