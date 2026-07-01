using FluentAssertions;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Services;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.UnitTests.Domain.Services;

public sealed class PnLCalculatorTests
{
    [Fact]
    public void CalculateFloatingPnL_ForLongPosition_ShouldUseBidPrice()
    {
        var position = new PositionCalculationState(netVolume: 10, averagePrice: 100);
        var quote = new Quote(
            Guid.NewGuid(),
            new Symbol("XAUUSD"),
            new Price(105),
            new Price(106),
            DateTimeOffset.UtcNow);

        var pnl = PnLCalculator.CalculateFloatingPnL(position, quote, contractSize: 2);

        pnl.Should().Be(100);
    }

    [Fact]
    public void CalculateFloatingPnL_ForShortPosition_ShouldUseAskPrice()
    {
        var position = new PositionCalculationState(netVolume: -10, averagePrice: 100);
        var quote = new Quote(
            Guid.NewGuid(),
            new Symbol("XAUUSD"),
            new Price(94),
            new Price(95),
            DateTimeOffset.UtcNow);

        var pnl = PnLCalculator.CalculateFloatingPnL(position, quote, contractSize: 2);

        pnl.Should().Be(100);
    }

    [Fact]
    public void CalculateFloatingPnL_ForFlatPosition_ShouldReturnZero()
    {
        var quote = new Quote(
            Guid.NewGuid(),
            new Symbol("EURUSD"),
            new Price(1.1000m),
            new Price(1.1002m),
            DateTimeOffset.UtcNow);

        var pnl = PnLCalculator.CalculateFloatingPnL(PositionCalculationState.Flat, quote, contractSize: 100000);

        pnl.Should().Be(0);
    }
}

