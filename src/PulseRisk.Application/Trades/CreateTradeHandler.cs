using FluentValidation;
using Microsoft.Extensions.Logging;
using PulseRisk.Application.Common;
using PulseRisk.Application.Events;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;
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
    TimeProvider timeProvider,
    IEventWriter<PositionChangedEvent> positionChangedEvents,
    ILogger<CreateTradeHandler> logger)
{
    private const int MaxPositionUpdateAttempts = 3;

    public async Task<TradeDto> HandleAsync(
        CreateTradeCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            logger.LogWarning(
                "Trade validation failed for client {ClientId}, account {TradingAccountId}: {ValidationErrors}.",
                command.ClientId,
                command.TradingAccountId,
                validationResult.Errors.Select(error => new
                {
                    error.PropertyName,
                    error.ErrorMessage
                }).ToArray());

            throw new ValidationException(validationResult.Errors);
        }

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

        for (var attempt = 1; attempt <= MaxPositionUpdateAttempts; attempt++)
        {
            try
            {
                return await CreateTradeAttemptAsync(
                    command.ClientId,
                    command.TradingAccountId,
                    symbol,
                    command.Side,
                    volume,
                    openPrice,
                    attempt,
                    cancellationToken);
            }
            catch (ConcurrencyConflictException exception) when (attempt < MaxPositionUpdateAttempts)
            {
                logger.LogWarning(
                    exception,
                    "Position concurrency conflict for account {TradingAccountId}, symbol {Symbol}. Retrying attempt {NextAttempt}/{MaxAttempts}.",
                    command.TradingAccountId,
                    symbol.Value,
                    attempt + 1,
                    MaxPositionUpdateAttempts);

                unitOfWork.ClearChanges();
            }
            catch (ConcurrencyConflictException exception)
            {
                unitOfWork.ClearChanges();

                logger.LogError(
                    exception,
                    "Position concurrency retry exhausted for account {TradingAccountId}, symbol {Symbol}.",
                    command.TradingAccountId,
                    symbol.Value);

                throw new ConflictException(
                    "Position was updated concurrently too many times. Please retry the trade.",
                    exception);
            }
        }

        throw new InvalidOperationException("Unreachable trade processing state.");
    }

    private async Task<TradeDto> CreateTradeAttemptAsync(
        Guid clientId,
        Guid tradingAccountId,
        Symbol symbol,
        TradeSide side,
        Volume volume,
        Price openPrice,
        int attempt,
        CancellationToken cancellationToken)
    {
        var timestamp = timeProvider.GetUtcNow();

        var trade = Trade.Create(
            clientId,
            tradingAccountId,
            symbol,
            side,
            volume,
            openPrice,
            timestamp);

        var position = await positions.GetByTradingAccountAndSymbolAsync(
            tradingAccountId,
            symbol,
            cancellationToken);

        if (position is null)
        {
            position = Position.CreateEmpty(
                clientId,
                tradingAccountId,
                symbol,
                timestamp);

            await positions.AddAsync(position, cancellationToken);
        }

        var updatedPosition = PositionCalculator.ApplyTrade(
            position.ToCalculationState(),
            side,
            volume,
            openPrice);

        position.Apply(updatedPosition, timestamp);

        await trades.AddAsync(trade, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await positionChangedEvents.WriteAsync(
            new PositionChangedEvent(
                trade.Id,
                position.Id,
                position.ClientId,
                position.TradingAccountId,
                position.Symbol.Value,
                position.NetVolume,
                position.AveragePrice,
                position.FloatingPnL,
                timestamp),
            cancellationToken);

        logger.LogInformation(
            "Trade accepted. TradeId {TradeId}, account {TradingAccountId}, symbol {Symbol}, side {Side}, volume {Volume}, attempt {Attempt}.",
            trade.Id,
            tradingAccountId,
            symbol.Value,
            side,
            volume.Value,
            attempt);

        logger.LogInformation(
            "Position updated. PositionId {PositionId}, account {TradingAccountId}, symbol {Symbol}, net volume {NetVolume}, average price {AveragePrice}.",
            position.Id,
            position.TradingAccountId,
            position.Symbol.Value,
            position.NetVolume,
            position.AveragePrice);

        return trade.ToDto();
    }
}
