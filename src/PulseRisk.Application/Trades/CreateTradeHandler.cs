using FluentValidation;
using PulseRisk.Application.Common;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Services;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Application.Trades;

public sealed class CreateTradeHandler(
    IValidator<CreateTradeCommand> validator,
    IClientRepository clients,
    ITradingAccountRepository accounts,
    IInstrumentRepository instruments,
    ITradeRepository trades,
    IPositionRepository positions,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<TradeDto> HandleAsync(
        CreateTradeCommand command,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        if (await clients.GetByIdAsync(command.ClientId, cancellationToken) is null)
        {
            throw new EntityNotFoundException("Client", command.ClientId.ToString());
        }

        var account = await accounts.GetByIdAsync(command.TradingAccountId, cancellationToken)
            ?? throw new EntityNotFoundException("TradingAccount", command.TradingAccountId.ToString());

        if (account.ClientId != command.ClientId)
        {
            throw new ConflictException(
                $"Trading account '{account.Id}' does not belong to client '{command.ClientId}'.");
        }

        var symbol = new Symbol(command.Symbol);
        var instrument = await instruments.GetBySymbolAsync(symbol, cancellationToken)
            ?? throw new EntityNotFoundException("Instrument", symbol.Value);

        if (!instrument.IsActive)
        {
            throw new ConflictException($"Instrument '{symbol}' is not active.");
        }

        var volume = new Volume(command.Volume);
        var openPrice = new Price(command.OpenPrice);
        var timestamp = timeProvider.GetUtcNow();

        var trade = Trade.Create(
            command.ClientId,
            command.TradingAccountId,
            symbol,
            command.Side,
            volume,
            openPrice,
            timestamp);

        var position = await positions.GetByTradingAccountAndSymbolAsync(
            command.TradingAccountId,
            symbol,
            cancellationToken);

        if (position is null)
        {
            position = Position.CreateEmpty(
                command.ClientId,
                command.TradingAccountId,
                symbol,
                timestamp);

            await positions.AddAsync(position, cancellationToken);
        }

        var updatedPosition = PositionCalculator.ApplyTrade(
            position.ToCalculationState(),
            command.Side,
            volume,
            openPrice);

        position.Apply(updatedPosition, timestamp);

        await trades.AddAsync(trade, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return trade.ToDto();
    }
}
