using PulseRisk.Domain.Common;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Domain.Entities;

public sealed class Trade
{
    private Trade()
    {
    }

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

    public Guid Id { get; private set; }

    public Guid ClientId { get; private set; }

    public Guid TradingAccountId { get; private set; }

    public Symbol Symbol { get; private set; }

    public TradeSide Side { get; private set; }

    public Volume Volume { get; private set; }

    public Price OpenPrice { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

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
