using FluentAssertions;
using PulseRisk.Application.Accounts;
using PulseRisk.Domain.Enums;

namespace PulseRisk.UnitTests.Application.Accounts;

public sealed class CreateTradingAccountValidatorTests
{
    private readonly CreateTradingAccountValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldPass()
    {
        var command = new CreateTradingAccountCommand(
            Guid.NewGuid(),
            Balance: 10000,
            CurrencyCode.USD,
            Leverage: 100);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenClientIdIsEmpty_ShouldFail()
    {
        var command = new CreateTradingAccountCommand(
            Guid.Empty,
            Balance: 10000,
            CurrencyCode.USD,
            Leverage: 100);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenLeverageIsZero_ShouldFail()
    {
        var command = new CreateTradingAccountCommand(
            Guid.NewGuid(),
            Balance: 10000,
            CurrencyCode.USD,
            Leverage: 0);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}

