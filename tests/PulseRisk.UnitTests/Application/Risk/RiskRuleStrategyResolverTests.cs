using FluentAssertions;
using PulseRisk.Application.Risk;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.Risk;
using PulseRisk.Domain.Risk.Strategies;

namespace PulseRisk.UnitTests.Application.Risk;

public sealed class RiskRuleStrategyResolverTests
{
    [Fact]
    public void TryResolve_WhenStrategyExists_ShouldReturnStrategy()
    {
        var resolver = new RiskRuleStrategyResolver([
            new MaxExposureRuleStrategy()
        ]);

        var resolved = resolver.TryResolve(RiskRuleType.MaxExposureLimit, out var strategy);

        resolved.Should().BeTrue();
        strategy.RuleType.Should().Be(RiskRuleType.MaxExposureLimit);
    }

    [Fact]
    public void TryResolve_WhenStrategyDoesNotExist_ShouldReturnFalse()
    {
        var resolver = new RiskRuleStrategyResolver(Array.Empty<IRiskRuleStrategy>());

        var resolved = resolver.TryResolve(RiskRuleType.PriceSpikeDetection, out _);

        resolved.Should().BeFalse();
    }
}
