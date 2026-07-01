using FluentAssertions;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.Services;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.UnitTests.Domain.Services;

public sealed class PositionCalculatorTests
{
    [Fact]
    public void ApplyTrade_WhenBuyTradeOpensFlatPosition_ShouldCreateLongPosition()
    {
        var result = PositionCalculator.ApplyTrade(
            PositionCalculationState.Flat,
            TradeSide.Buy,
            new Volume(10),
            new Price(1.1000m));

        result.NetVolume.Should().Be(10);
        result.AveragePrice.Should().Be(1.1000m);
    }

    [Fact]
    public void ApplyTrade_WhenAdditionalBuyIncreasesLongPosition_ShouldRecalculateWeightedAveragePrice()
    {
        var current = new PositionCalculationState(netVolume: 10, averagePrice: 1.1000m);

        var result = PositionCalculator.ApplyTrade(
            current,
            TradeSide.Buy,
            new Volume(10),
            new Price(1.2000m));

        result.NetVolume.Should().Be(20);
        result.AveragePrice.Should().Be(1.1500m);
    }

    [Fact]
    public void ApplyTrade_WhenSellPartiallyClosesLongPosition_ShouldKeepAveragePrice()
    {
        var current = new PositionCalculationState(netVolume: 10, averagePrice: 1.1000m);

        var result = PositionCalculator.ApplyTrade(
            current,
            TradeSide.Sell,
            new Volume(4),
            new Price(1.2000m));

        result.NetVolume.Should().Be(6);
        result.AveragePrice.Should().Be(1.1000m);
    }

    [Fact]
    public void ApplyTrade_WhenSellExactlyClosesLongPosition_ShouldResetAveragePrice()
    {
        var current = new PositionCalculationState(netVolume: 10, averagePrice: 1.1000m);

        var result = PositionCalculator.ApplyTrade(
            current,
            TradeSide.Sell,
            new Volume(10),
            new Price(1.2000m));

        result.NetVolume.Should().Be(0);
        result.AveragePrice.Should().Be(0);
    }

    [Fact]
    public void ApplyTrade_WhenSellFlipsLongPositionToShort_ShouldUseTradePriceAsNewAveragePrice()
    {
        var current = new PositionCalculationState(netVolume: 10, averagePrice: 1.1000m);

        var result = PositionCalculator.ApplyTrade(
            current,
            TradeSide.Sell,
            new Volume(15),
            new Price(1.2000m));

        result.NetVolume.Should().Be(-5);
        result.AveragePrice.Should().Be(1.2000m);
    }

    [Fact]
    public void ApplyTrade_WhenAdditionalSellIncreasesShortPosition_ShouldRecalculateWeightedAveragePrice()
    {
        var current = new PositionCalculationState(netVolume: -10, averagePrice: 1.2000m);

        var result = PositionCalculator.ApplyTrade(
            current,
            TradeSide.Sell,
            new Volume(10),
            new Price(1.1000m));

        result.NetVolume.Should().Be(-20);
        result.AveragePrice.Should().Be(1.1500m);
    }
}

