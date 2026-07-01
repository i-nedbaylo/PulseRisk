using FluentAssertions;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.UnitTests.Domain.Entities;

public sealed class QuoteTests
{
    [Fact]
    public void Constructor_WhenBidIsGreaterThanAsk_ShouldThrow()
    {
        var act = () => new Quote(
            Guid.NewGuid(),
            new Symbol("EURUSD"),
            new Price(1.1010m),
            new Price(1.1000m),
            DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }
}

