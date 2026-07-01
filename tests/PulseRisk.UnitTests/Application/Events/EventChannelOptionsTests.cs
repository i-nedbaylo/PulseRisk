using FluentAssertions;
using PulseRisk.Application.Events;

namespace PulseRisk.UnitTests.Application.Events;

public sealed class EventChannelOptionsTests
{
    [Fact]
    public void GetSettingsFor_ShouldUseQuoteSettingsForQuoteTick()
    {
        var options = new EventChannelOptions
        {
            QuoteCapacity = 2048,
            QuoteFullMode = EventChannelFullMode.DropOldest
        };

        var settings = options.GetSettingsFor<QuoteTick>();

        settings.Capacity.Should().Be(2048);
        settings.FullMode.Should().Be(EventChannelFullMode.DropOldest);
    }

    [Fact]
    public void GetSettingsFor_ShouldUseRiskSettingsForRiskEvents()
    {
        var options = new EventChannelOptions
        {
            RiskCapacity = 128,
            RiskFullMode = EventChannelFullMode.Wait
        };

        var positionSettings = options.GetSettingsFor<PositionChangedEvent>();
        var riskRequestSettings = options.GetSettingsFor<RiskEvaluationRequested>();

        positionSettings.Should().Be(new EventChannelSettings(128, EventChannelFullMode.Wait));
        riskRequestSettings.Should().Be(new EventChannelSettings(128, EventChannelFullMode.Wait));
    }

    [Fact]
    public void GetSettingsFor_WhenCapacityIsInvalid_ShouldNormalizeToOne()
    {
        var options = new EventChannelOptions
        {
            DefaultCapacity = 0
        };

        var settings = options.GetSettingsFor<TradeAcceptedEvent>();

        settings.Capacity.Should().Be(1);
    }
}
