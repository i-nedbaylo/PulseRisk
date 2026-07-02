using FluentAssertions;
using PulseRisk.BackgroundWorkers.LoadTests;

namespace PulseRisk.UnitTests.BackgroundWorkers.LoadTests;

public sealed class LoadTestOptionsValidatorTests
{
    private readonly LoadTestOptionsValidator _validator = new();

    [Fact]
    public void Validate_WhenLoadTestIsDisabled_ShouldAllowDefaultOptions()
    {
        var result = _validator.Validate(null, new LoadTestOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenEnabledOptionsAreValid_ShouldSucceed()
    {
        var result = _validator.Validate(
            null,
            new LoadTestOptions
            {
                Enabled = true,
                ClientCount = 2,
                AccountsPerClient = 1,
                DurationSeconds = 1,
                DrainSeconds = 1,
                Symbols = ["EURUSD"],
                ReportPath = "docs/load-tests/test.md"
            });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenEnabledOptionsAreInvalid_ShouldReturnFailures()
    {
        var result = _validator.Validate(
            null,
            new LoadTestOptions
            {
                Enabled = true,
                ClientCount = 0,
                AccountsPerClient = 0,
                DurationSeconds = 0,
                DrainSeconds = -1,
                InitialBalance = 0,
                Leverage = 0,
                Symbols = [],
                ReportPath = ""
            });

        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("ClientCount", StringComparison.Ordinal));
        result.Failures.Should().Contain(failure => failure.Contains("Symbols", StringComparison.Ordinal));
        result.Failures.Should().Contain(failure => failure.Contains("ReportPath", StringComparison.Ordinal));
    }
}
