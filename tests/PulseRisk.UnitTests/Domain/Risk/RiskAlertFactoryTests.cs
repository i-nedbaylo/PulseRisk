using FluentAssertions;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.Risk;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.UnitTests.Domain.Risk;

public sealed class RiskAlertFactoryTests
{
    [Fact]
    public void Create_WhenResultIsTriggered_ShouldCreateActiveAlert()
    {
        var clientId = Guid.NewGuid();
        var tradingAccountId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);
        var context = new RiskEvaluationContext(
            clientId,
            tradingAccountId,
            new Symbol("EURUSD"),
            NetExposure: 150_000m,
            FloatingPnL: -1_000m,
            MarginLevel: 80m);
        var result = RiskRuleEvaluationResult.Triggered(
            RiskAlertType.ExposureLimitExceeded,
            RiskSeverity.Critical,
            "Exposure limit exceeded.");
        var factory = new RiskAlertFactory();

        var alert = factory.Create(context, result, createdAt);

        alert.Id.Should().NotBeEmpty();
        alert.ClientId.Should().Be(clientId);
        alert.TradingAccountId.Should().Be(tradingAccountId);
        alert.Symbol.Value.Should().Be("EURUSD");
        alert.AlertType.Should().Be(RiskAlertType.ExposureLimitExceeded);
        alert.Severity.Should().Be(RiskSeverity.Critical);
        alert.Message.Should().Be("Exposure limit exceeded.");
        alert.CreatedAt.Should().Be(createdAt);
        alert.ResolvedAt.Should().BeNull();
        alert.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WhenResultIsNotTriggered_ShouldThrow()
    {
        var context = new RiskEvaluationContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Symbol("EURUSD"),
            NetExposure: 10_000m,
            FloatingPnL: 0m,
            MarginLevel: 200m);
        var factory = new RiskAlertFactory();

        var act = () => factory.Create(
            context,
            RiskRuleEvaluationResult.NotTriggered(),
            DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Only triggered rule results can create risk alerts.*");
    }

    [Fact]
    public void Create_WhenTriggeredResultHasBlankMessage_ShouldThrow()
    {
        var context = new RiskEvaluationContext(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Symbol("EURUSD"),
            NetExposure: 150_000m,
            FloatingPnL: -1_000m,
            MarginLevel: 80m);
        var result = RiskRuleEvaluationResult.Triggered(
            RiskAlertType.ExposureLimitExceeded,
            RiskSeverity.Critical,
            " ");
        var factory = new RiskAlertFactory();

        var act = () => factory.Create(context, result, DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Triggered rule result must contain alert message.*");
    }
}
