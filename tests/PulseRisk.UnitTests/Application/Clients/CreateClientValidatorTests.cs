using FluentAssertions;
using PulseRisk.Application.Clients;

namespace PulseRisk.UnitTests.Application.Clients;

public sealed class CreateClientValidatorTests
{
    private readonly CreateClientValidator _validator = new();

    [Fact]
    public void Validate_WhenNameIsValid_ShouldPass()
    {
        var result = _validator.Validate(new CreateClientCommand("Acme Capital"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldFail()
    {
        var result = _validator.Validate(new CreateClientCommand(""));

        result.IsValid.Should().BeFalse();
    }
}

