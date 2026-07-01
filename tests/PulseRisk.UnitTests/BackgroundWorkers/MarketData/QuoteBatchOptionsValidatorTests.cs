using FluentAssertions;
using PulseRisk.BackgroundWorkers.MarketData;

namespace PulseRisk.UnitTests.BackgroundWorkers.MarketData;

public sealed class QuoteBatchOptionsValidatorTests
{
    [Fact]
    public void Validate_WhenBatchSizeIsOutsideSupportedRange_ShouldFail()
    {
        var validator = new QuoteBatchOptionsValidator();
        var options = new QuoteBatchOptions
        {
            BatchSize = 0
        };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("BatchSize"));
    }

    [Fact]
    public void Validate_WhenFlushIntervalIsOutsideSupportedRange_ShouldFail()
    {
        var validator = new QuoteBatchOptionsValidator();
        var options = new QuoteBatchOptions
        {
            FlushIntervalMilliseconds = 1
        };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("FlushIntervalMilliseconds"));
    }
}
