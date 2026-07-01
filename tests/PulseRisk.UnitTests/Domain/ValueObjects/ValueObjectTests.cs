using FluentAssertions;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.UnitTests.Domain.ValueObjects;

public sealed class ValueObjectTests
{
    [Fact]
    public void Symbol_ShouldTrimAndNormalizeToUppercase()
    {
        var symbol = new Symbol(" eurusd ");

        symbol.Value.Should().Be("EURUSD");
    }

    [Fact]
    public void Price_WhenValueIsZero_ShouldThrow()
    {
        var act = () => new Price(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Volume_WhenValueIsNegative_ShouldThrow()
    {
        var act = () => new Volume(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

