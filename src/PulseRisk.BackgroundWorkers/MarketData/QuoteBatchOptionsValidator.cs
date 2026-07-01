using Microsoft.Extensions.Options;

namespace PulseRisk.BackgroundWorkers.MarketData;

public sealed class QuoteBatchOptionsValidator : IValidateOptions<QuoteBatchOptions>
{
    public ValidateOptionsResult Validate(string? name, QuoteBatchOptions options)
    {
        var failures = new List<string>();

        if (options.BatchSize is < 1 or > 10_000)
        {
            failures.Add("QuoteBatch:BatchSize must be between 1 and 10000.");
        }

        if (options.FlushIntervalMilliseconds is < 10 or > 60_000)
        {
            failures.Add("QuoteBatch:FlushIntervalMilliseconds must be between 10 and 60000.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
