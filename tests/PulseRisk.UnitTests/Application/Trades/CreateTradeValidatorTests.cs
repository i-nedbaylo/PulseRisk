using FluentAssertions;
using PulseRisk.Application.Trades;
using PulseRisk.Domain.Enums;

namespace PulseRisk.UnitTests.Application.Trades;

public sealed class CreateTradeValidatorTests
{
    private readonly CreateTradeValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldPass()
    {
        var command = new CreateTradeCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "EURUSD",
            TradeSide.Buy,
            Volume: 1,
            OpenPrice: 1.10025m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenTradingAccountIdIsEmpty_ShouldFail()
    {
        var command = new CreateTradeCommand(
            Guid.NewGuid(),
            Guid.Empty,
            "EURUSD",
            TradeSide.Buy,
            Volume: 1,
            OpenPrice: 1.10025m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenSideIsUnknown_ShouldFail()
    {
        var command = new CreateTradeCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "EURUSD",
            (TradeSide)999,
            Volume: 1,
            OpenPrice: 1.10025m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenVolumeOrPriceIsNotPositive_ShouldFail()
    {
        var command = new CreateTradeCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "EURUSD",
            TradeSide.Buy,
            Volume: 0,
            OpenPrice: 0);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
