using FluentValidation;
using PulseRisk.Application.Common;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Application.Instruments;

public sealed class CreateInstrumentHandler(
    IValidator<CreateInstrumentCommand> validator,
    IInstrumentRepository instruments,
    IUnitOfWork unitOfWork)
{
    public async Task<InstrumentDto> HandleAsync(
        CreateInstrumentCommand command,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var symbol = new Symbol(command.Symbol);
        if (await instruments.GetBySymbolAsync(symbol, cancellationToken) is not null)
        {
            throw new ConflictException($"Instrument '{symbol}' already exists.");
        }

        var instrument = new Instrument(
            symbol,
            command.BaseAsset,
            command.QuoteAsset,
            command.Digits,
            command.ContractSize,
            command.IsActive);

        await instruments.AddAsync(instrument, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return instrument.ToDto();
    }
}

