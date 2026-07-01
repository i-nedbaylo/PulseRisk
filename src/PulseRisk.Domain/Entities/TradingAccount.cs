using PulseRisk.Domain.Common;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Domain.Entities;

public sealed class TradingAccount
{
    private TradingAccount()
    {
    }

    public TradingAccount(
        Guid id,
        Guid clientId,
        Money balance,
        decimal leverage,
        DateTimeOffset createdAt)
    {
        DomainValidation.EnsureNotEmpty(id, nameof(id));
        DomainValidation.EnsureNotEmpty(clientId, nameof(clientId));

        if (leverage <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(leverage), "Leverage must be greater than zero.");
        }

        Id = id;
        ClientId = clientId;
        Balance = balance.Amount;
        Currency = balance.Currency;
        Leverage = leverage;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ClientId { get; private set; }

    public decimal Balance { get; private set; }

    public CurrencyCode Currency { get; private set; }

    public decimal Leverage { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void UpdateBalance(Money balance)
    {
        if (balance.Currency != Currency)
        {
            throw new InvalidOperationException("Trading account currency cannot be changed.");
        }

        Balance = balance.Amount;
    }
}
