using FluentAssertions;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.Risk;
using PulseRisk.Domain.Risk.Strategies;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.UnitTests.Domain.Risk.Strategies;

public sealed class MaxExposureRuleStrategyTests
{
    [Fact]
    public async Task EvaluateAsync_WhenExposureExceedsThreshold_ShouldTrigger()
    {
        var strategy = new MaxExposureRuleStrategy();
        var context = CreateContext(netExposure: 150_000m);
        var rule = CreateRule(threshold: 100_000m);

        var result = await strategy.EvaluateAsync(context, rule, CancellationToken.None);

        result.IsTriggered.Should().BeTrue();
        result.AlertType.Should().Be(RiskAlertType.ExposureLimitExceeded);
        result.Severity.Should().Be(RiskSeverity.Critical);
        result.Message.Should().Contain("Net exposure");
    }

    [Fact]
    public async Task EvaluateAsync_WhenExposureIsAtThreshold_ShouldNotTrigger()
    {
        var strategy = new MaxExposureRuleStrategy();
        var context = CreateContext(netExposure: 100_000m);
        var rule = CreateRule(threshold: 100_000m);

        var result = await strategy.EvaluateAsync(context, rule, CancellationToken.None);

        result.IsTriggered.Should().BeFalse();
    }

    private static RiskEvaluationContext CreateContext(decimal netExposure)
    {
        return new RiskEvaluationContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Symbol("EURUSD"),
            netExposure,
            FloatingPnL: 0m,
            MarginLevel: 200m);
    }

    private static RiskRule CreateRule(decimal threshold)
    {
        return new RiskRule(
            Guid.NewGuid(),
            "Max exposure",
            RiskRuleType.MaxExposureLimit,
            threshold,
            RiskSeverity.Critical,
            isEnabled: true);
    }
}
