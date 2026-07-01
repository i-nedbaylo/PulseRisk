using FluentAssertions;
using PulseRisk.BackgroundWorkers.MarketData;

namespace PulseRisk.UnitTests.BackgroundWorkers.MarketData;

public sealed class MarketDataQuoteGeneratorTests
{
    [Fact]
    public void NextTick_ShouldProduceValidQuoteTick()
    {
        var generator = new MarketDataQuoteGenerator(new Random(42));
        var options = new MarketDataOptions();
        var timestamp = new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);

        var tick = generator.NextTick("eurusd", options, timestamp);

        tick.Symbol.Should().Be("EURUSD");
        tick.Bid.Should().BeGreaterThan(0);
        tick.Ask.Should().BeGreaterThan(0);
        tick.Bid.Should().BeLessThanOrEqualTo(tick.Ask);
        tick.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void NextTick_ShouldMoveFromPreviousMidPrice()
    {
        var generator = new MarketDataQuoteGenerator(new Random(42));
        var options = new MarketDataOptions
        {
            PriceStepPercent = 0.001m
        };
        var firstTimestamp = new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);
        var secondTimestamp = firstTimestamp.AddSeconds(1);

        var firstTick = generator.NextTick("GBPUSD", options, firstTimestamp);
        var secondTick = generator.NextTick("GBPUSD", options, secondTimestamp);

        secondTick.Symbol.Should().Be(firstTick.Symbol);
        secondTick.Timestamp.Should().Be(secondTimestamp);
        secondTick.Bid.Should().NotBe(firstTick.Bid);
    }
}
