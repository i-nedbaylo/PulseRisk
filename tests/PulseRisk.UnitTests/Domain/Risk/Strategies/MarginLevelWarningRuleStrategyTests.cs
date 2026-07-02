using FluentAssertions;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.Risk;
using PulseRisk.Domain.Risk.Strategies;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.UnitTests.Domain.Risk.Strategies;

public sealed class MarginLevelWarningRuleStrategyTests
{
    [Fact]
    public async Task EvaluateAsync_WhenMarginLevelIsAtOrBelowThreshold_ShouldTrigger()
    {
        var strategy = new MarginLevelWarningRuleStrategy();
        var context = CreateContext(marginLevel: 80m);
        var rule = CreateRule(threshold: 100m);

        var result = await strategy.EvaluateAsync(context, rule, CancellationToken.None);

        result.IsTriggered.Should().BeTrue();
        result.AlertType.Should().Be(RiskAlertType.MarginLevelWarning);
        result.Severity.Should().Be(RiskSeverity.Warning);
        result.Message.Should().Contain("Margin level");
    }

    [Fact]
    public async Task EvaluateAsync_WhenMarginLevelIsAboveThreshold_ShouldNotTrigger()
    {
        var strategy = new MarginLevelWarningRuleStrategy();
        var context = CreateContext(marginLevel: 150m);
        var rule = CreateRule(threshold: 100m);

        var result = await strategy.EvaluateAsync(context, rule, CancellationToken.None);

        result.IsTriggered.Should().BeFalse();
    }

    private static RiskEvaluationContext CreateContext(decimal marginLevel)
    {
        return new RiskEvaluationContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Symbol("EURUSD"),
            NetExposure: 100_000m,
            FloatingPnL: 0m,
            marginLevel);
    }

    private static RiskRule CreateRule(decimal threshold)
    {
        return new RiskRule(
            Guid.NewGuid(),
            "Margin warning",
            RiskRuleType.MarginLevelWarning,
            threshold,
            RiskSeverity.Warning,
            isEnabled: true);
    }
}
