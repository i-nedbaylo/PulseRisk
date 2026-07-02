namespace PulseRisk.BackgroundWorkers.LoadTests;

public sealed record LoadTestScenarioSettings(
    bool Enabled,
    bool StopApplicationWhenCompleted,
    LoadTestProfile Profile,
    int ClientCount,
    int AccountsPerClient,
    TimeSpan Duration,
    TimeSpan DrainTime,
    int QuotesPerSecond,
    int TradesPerSecond,
    decimal InitialBalance,
    decimal Leverage,
    IReadOnlyCollection<string> Symbols,
    string ReportPath);
