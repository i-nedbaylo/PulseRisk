using FluentAssertions;
using PulseRisk.BackgroundWorkers.MarketData;

namespace PulseRisk.UnitTests.BackgroundWorkers.MarketData;

public sealed class MarketDataOptionsValidatorTests
{
    [Fact]
    public void Validate_WhenEnabledWithoutSymbols_ShouldFail()
    {
        var validator = new MarketDataOptionsValidator();
        var options = new MarketDataOptions
        {
            Enabled = true,
            Symbols = []
        };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("Symbols"));
    }

    [Fact]
    public void Validate_WhenFrequencyIsOutsideSupportedRange_ShouldFail()
    {
        var validator = new MarketDataOptionsValidator();
        var options = new MarketDataOptions
        {
            TicksPerSecondPerInstrument = 0
        };

        var result = validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("TicksPerSecondPerInstrument"));
    }
}
