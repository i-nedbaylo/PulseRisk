using Microsoft.Extensions.Options;

namespace PulseRisk.BackgroundWorkers.MarketData;

public sealed class MarketDataOptionsValidator : IValidateOptions<MarketDataOptions>
{
    public ValidateOptionsResult Validate(string? name, MarketDataOptions options)
    {
        var failures = new List<string>();

        if (options.Enabled && options.GetNormalizedSymbols().Count == 0)
        {
            failures.Add("MarketData:Symbols must contain at least one symbol when simulator is enabled.");
        }

        if (options.TicksPerSecondPerInstrument is < 1 or > 1000)
        {
            failures.Add("MarketData:TicksPerSecondPerInstrument must be between 1 and 1000.");
        }

        if (options.PriceStepPercent <= 0 || options.PriceStepPercent > 0.10m)
        {
            failures.Add("MarketData:PriceStepPercent must be greater than 0 and not greater than 0.10.");
        }

        if (options.SpreadPercent <= 0 || options.SpreadPercent > 0.10m)
        {
            failures.Add("MarketData:SpreadPercent must be greater than 0 and not greater than 0.10.");
        }

        if (options.MinimumPrice <= 0)
        {
            failures.Add("MarketData:MinimumPrice must be greater than 0.");
        }

        if (options.StatisticsIntervalSeconds <= 0)
        {
            failures.Add("MarketData:StatisticsIntervalSeconds must be greater than 0.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
