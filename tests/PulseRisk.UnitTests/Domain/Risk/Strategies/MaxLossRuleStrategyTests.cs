using FluentAssertions;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.Risk;
using PulseRisk.Domain.Risk.Strategies;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.UnitTests.Domain.Risk.Strategies;

public sealed class MaxLossRuleStrategyTests
{
    [Fact]
    public async Task EvaluateAsync_WhenFloatingPnlIsAtOrBelowLossThreshold_ShouldTrigger()
    {
        var strategy = new MaxLossRuleStrategy();
        var context = CreateContext(floatingPnL: -30_000m);
        var rule = CreateRule(threshold: -25_000m);

        var result = await strategy.EvaluateAsync(context, rule, CancellationToken.None);

        result.IsTriggered.Should().BeTrue();
        result.AlertType.Should().Be(RiskAlertType.MaxLossExceeded);
        result.Severity.Should().Be(RiskSeverity.Critical);
        result.Message.Should().Contain("Floating PnL");
    }

    [Fact]
    public async Task EvaluateAsync_WhenFloatingPnlIsAtLossThreshold_ShouldTrigger()
    {
        var strategy = new MaxLossRuleStrategy();
        var context = CreateContext(floatingPnL: -25_000m);
        var rule = CreateRule(threshold: -25_000m);

        var result = await strategy.EvaluateAsync(context, rule, CancellationToken.None);

        result.IsTriggered.Should().BeTrue();
        result.AlertType.Should().Be(RiskAlertType.MaxLossExceeded);
    }

    [Fact]
    public async Task EvaluateAsync_WhenFloatingPnlIsAboveLossThreshold_ShouldNotTrigger()
    {
        var strategy = new MaxLossRuleStrategy();
        var context = CreateContext(floatingPnL: -10_000m);
        var rule = CreateRule(threshold: -25_000m);

        var result = await strategy.EvaluateAsync(context, rule, CancellationToken.None);

        result.IsTriggered.Should().BeFalse();
    }

    private static RiskEvaluationContext CreateContext(decimal floatingPnL)
    {
        return new RiskEvaluationContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Symbol("EURUSD"),
            NetExposure: 100_000m,
            floatingPnL,
            MarginLevel: 200m);
    }

    private static RiskRule CreateRule(decimal threshold)
    {
        return new RiskRule(
            Guid.NewGuid(),
            "Max loss",
            RiskRuleType.MaxLossLimit,
            threshold,
            RiskSeverity.Critical,
            isEnabled: true);
    }
}
