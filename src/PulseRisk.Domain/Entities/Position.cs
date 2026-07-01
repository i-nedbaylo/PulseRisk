using PulseRisk.Domain.Common;
using PulseRisk.Domain.Services;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Domain.Entities;

public sealed class Position
{
    private Position()
    {
    }

    public Position(
        Guid id,
        Guid clientId,
        Guid tradingAccountId,
        Symbol symbol,
        decimal netVolume,
        decimal averagePrice,
        decimal floatingPnL,
        DateTimeOffset updatedAt)
    {
        DomainValidation.EnsureNotEmpty(id, nameof(id));
        DomainValidation.EnsureNotEmpty(clientId, nameof(clientId));
        DomainValidation.EnsureNotEmpty(tradingAccountId, nameof(tradingAccountId));

        if (averagePrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(averagePrice), "Average price must not be negative.");
        }

        if (netVolume == 0 && averagePrice != 0)
        {
            throw new ArgumentException("Flat position must have zero average price.", nameof(averagePrice));
        }

        Id = id;
        ClientId = clientId;
        TradingAccountId = tradingAccountId;
        Symbol = symbol;
        NetVolume = netVolume;
        AveragePrice = averagePrice;
        FloatingPnL = floatingPnL;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid ClientId { get; private set; }

    public Guid TradingAccountId { get; private set; }

    public Symbol Symbol { get; private set; }

    public decimal NetVolume { get; private set; }

    public decimal AveragePrice { get; private set; }

    public decimal FloatingPnL { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Position CreateEmpty(
        Guid clientId,
        Guid tradingAccountId,
        Symbol symbol,
        DateTimeOffset createdAt)
    {
        return new Position(
            Guid.NewGuid(),
            clientId,
            tradingAccountId,
            symbol,
            netVolume: 0,
            averagePrice: 0,
            floatingPnL: 0,
            updatedAt: createdAt);
    }

    public PositionCalculationState ToCalculationState()
    {
        return new PositionCalculationState(NetVolume, AveragePrice);
    }

    public void Apply(PositionCalculationResult result, DateTimeOffset updatedAt)
    {
        NetVolume = result.NetVolume;
        AveragePrice = result.AveragePrice;
        UpdatedAt = updatedAt;
    }

    public void UpdateFloatingPnL(decimal floatingPnL, DateTimeOffset updatedAt)
    {
        FloatingPnL = floatingPnL;
        UpdatedAt = updatedAt;
    }
}
