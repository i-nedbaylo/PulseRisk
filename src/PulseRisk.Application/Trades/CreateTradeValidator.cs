using FluentValidation;

namespace PulseRisk.Application.Trades;

public sealed class CreateTradeValidator : AbstractValidator<CreateTradeCommand>
{
    public CreateTradeValidator()
    {
        RuleFor(command => command.ClientId)
            .NotEmpty();

        RuleFor(command => command.TradingAccountId)
            .NotEmpty();

        RuleFor(command => command.Symbol)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(command => command.Side)
            .IsInEnum();

        RuleFor(command => command.Volume)
            .GreaterThan(0);

        RuleFor(command => command.OpenPrice)
            .GreaterThan(0);
    }
}
