using FluentAssertions;
using PulseRisk.Application.Instruments;

namespace PulseRisk.UnitTests.Application.Instruments;

public sealed class CreateInstrumentValidatorTests
{
    private readonly CreateInstrumentValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldPass()
    {
        var command = new CreateInstrumentCommand(
            "EURUSD",
            "EUR",
            "USD",
            Digits: 5,
            ContractSize: 100000);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenSymbolIsEmpty_ShouldFail()
    {
        var command = new CreateInstrumentCommand(
            "",
            "EUR",
            "USD",
            Digits: 5,
            ContractSize: 100000);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenContractSizeIsZero_ShouldFail()
    {
        var command = new CreateInstrumentCommand(
            "EURUSD",
            "EUR",
            "USD",
            Digits: 5,
            ContractSize: 0);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}

