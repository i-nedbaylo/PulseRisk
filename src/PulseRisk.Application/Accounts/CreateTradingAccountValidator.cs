using FluentValidation;

namespace PulseRisk.Application.Accounts;

public sealed class CreateTradingAccountValidator : AbstractValidator<CreateTradingAccountCommand>
{
    public CreateTradingAccountValidator()
    {
        RuleFor(command => command.ClientId)
            .NotEmpty();

        RuleFor(command => command.Balance)
            .GreaterThanOrEqualTo(0);

        RuleFor(command => command.Currency)
            .IsInEnum();

        RuleFor(command => command.Leverage)
            .GreaterThan(0);
    }
}

