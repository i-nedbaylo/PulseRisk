using Microsoft.Extensions.Options;

namespace PulseRisk.BackgroundWorkers.LoadTests;

public sealed class LoadTestOptionsValidator : IValidateOptions<LoadTestOptions>
{
    public ValidateOptionsResult Validate(string? name, LoadTestOptions options)
    {
        var failures = new List<string>();

        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (options.GetNormalizedSymbols().Count == 0)
        {
            failures.Add("LoadTest:Symbols must contain at least one symbol when load test is enabled.");
        }

        if (options.ClientCount is < 1 or > 10_000)
        {
            failures.Add("LoadTest:ClientCount must be between 1 and 10000.");
        }

        if (options.AccountsPerClient is < 1 or > 20)
        {
            failures.Add("LoadTest:AccountsPerClient must be between 1 and 20.");
        }

        if (options.DurationSeconds is < 1 or > 3600)
        {
            failures.Add("LoadTest:DurationSeconds must be between 1 and 3600.");
        }

        if (options.DrainSeconds is < 0 or > 300)
        {
            failures.Add("LoadTest:DrainSeconds must be between 0 and 300.");
        }

        if (options.QuotesPerSecond is < 0 or > 20_000)
        {
            failures.Add("LoadTest:QuotesPerSecond must be between 0 and 20000.");
        }

        if (options.TradesPerSecond is < 0 or > 2_000)
        {
            failures.Add("LoadTest:TradesPerSecond must be between 0 and 2000.");
        }

        if (options.InitialBalance <= 0)
        {
            failures.Add("LoadTest:InitialBalance must be greater than 0.");
        }

        if (options.Leverage <= 0)
        {
            failures.Add("LoadTest:Leverage must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(options.ReportPath))
        {
            failures.Add("LoadTest:ReportPath must not be empty.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
