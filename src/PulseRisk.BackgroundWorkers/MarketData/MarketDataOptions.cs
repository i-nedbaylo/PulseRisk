namespace PulseRisk.BackgroundWorkers.MarketData;

public sealed class MarketDataOptions
{
    public const string SectionName = "MarketData";

    public bool Enabled { get; init; }

    public string[] Symbols { get; init; } = ["EURUSD", "GBPUSD", "XAUUSD"];

    public int TicksPerSecondPerInstrument { get; init; } = 2;

    public decimal PriceStepPercent { get; init; } = 0.0002m;

    public decimal SpreadPercent { get; init; } = 0.0001m;

    public decimal MinimumPrice { get; init; } = 0.0001m;

    public int StatisticsIntervalSeconds { get; init; } = 10;

    public IReadOnlyCollection<string> GetNormalizedSymbols()
    {
        return Symbols
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
