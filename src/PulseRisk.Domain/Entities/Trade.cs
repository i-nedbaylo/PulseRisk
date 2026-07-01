using PulseRisk.Domain.Common;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Domain.Entities;

public sealed class Trade
{
    public Trade(
        Guid id,
        Guid clientId,
        Guid tradingAccountId,
        Symbol symbol,
        TradeSide side,
        Volume volume,
        Price openPrice,
        DateTimeOffset createdAt)
    {
        DomainValidation.EnsureNotEmpty(id, nameof(id));
        DomainValidation.EnsureNotEmpty(clientId, nameof(clientId));
        DomainValidation.EnsureNotEmpty(tradingAccountId, nameof(tradingAccountId));

        Id = id;
        ClientId = clientId;
        TradingAccountId = tradingAccountId;
        Symbol = symbol;
        Side = side;
        Volume = volume;
        OpenPrice = openPrice;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid ClientId { get; }

    public Guid TradingAccountId { get; }

    public Symbol Symbol { get; }

    public TradeSide Side { get; }

    public Volume Volume { get; }

    public Price OpenPrice { get; }

    public DateTimeOffset CreatedAt { get; }

    public static Trade Create(
        Guid clientId,
        Guid tradingAccountId,
        Symbol symbol,
        TradeSide side,
        Volume volume,
        Price openPrice,
        DateTimeOffset createdAt)
    {
        return new Trade(
            Guid.NewGuid(),
            clientId,
            tradingAccountId,
            symbol,
            side,
            volume,
            openPrice,
            createdAt);
    }
}

