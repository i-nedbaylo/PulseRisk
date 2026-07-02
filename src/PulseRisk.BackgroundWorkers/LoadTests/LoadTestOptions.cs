namespace PulseRisk.BackgroundWorkers.LoadTests;

public sealed class LoadTestOptions
{
    public const string SectionName = "LoadTest";

    public bool Enabled { get; init; }

    public bool StopApplicationWhenCompleted { get; init; }

    public LoadTestProfile Profile { get; init; } = LoadTestProfile.Quotes500;

    public int ClientCount { get; init; } = 10;

    public int AccountsPerClient { get; init; } = 1;

    public int DurationSeconds { get; init; } = 30;

    public int DrainSeconds { get; init; } = 5;

    public int QuotesPerSecond { get; init; }

    public int TradesPerSecond { get; init; }

    public decimal InitialBalance { get; init; } = 100_000m;

    public decimal Leverage { get; init; } = 100m;

    public string[] Symbols { get; init; } = ["EURUSD", "GBPUSD", "XAUUSD"];

    public string ReportPath { get; init; } = "docs/load-tests/latest-local-load-report.md";

    public IReadOnlyCollection<string> GetNormalizedSymbols()
    {
        return Symbols
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public LoadTestScenarioSettings ToScenarioSettings()
    {
        return new LoadTestScenarioSettings(
            Enabled,
            StopApplicationWhenCompleted,
            Profile,
            ClientCount,
            AccountsPerClient,
            TimeSpan.FromSeconds(DurationSeconds),
            TimeSpan.FromSeconds(DrainSeconds),
            GetEffectiveQuotesPerSecond(),
            GetEffectiveTradesPerSecond(),
            InitialBalance,
            Leverage,
            GetNormalizedSymbols().ToArray(),
            ReportPath);
    }

    private int GetEffectiveQuotesPerSecond()
    {
        if (QuotesPerSecond > 0)
        {
            return QuotesPerSecond;
        }

        return Profile switch
        {
            LoadTestProfile.Quotes5000 => 5000,
            LoadTestProfile.Quotes1000 => 1000,
            _ => 500
        };
    }

    private int GetEffectiveTradesPerSecond()
    {
        if (TradesPerSecond > 0)
        {
            return TradesPerSecond;
        }

        return Profile switch
        {
            LoadTestProfile.Quotes5000 => 100,
            LoadTestProfile.Quotes1000 => 50,
            _ => 25
        };
    }
}
