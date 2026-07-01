using FluentValidation;

namespace PulseRisk.Application.Instruments;

public sealed class CreateInstrumentValidator : AbstractValidator<CreateInstrumentCommand>
{
    public CreateInstrumentValidator()
    {
        RuleFor(command => command.Symbol)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(command => command.BaseAsset)
            .NotEmpty()
            .MaximumLength(16);

        RuleFor(command => command.QuoteAsset)
            .NotEmpty()
            .MaximumLength(16);

        RuleFor(command => command.Digits)
            .GreaterThanOrEqualTo(0);

        RuleFor(command => command.ContractSize)
            .GreaterThan(0);
    }
}

