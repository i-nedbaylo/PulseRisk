using PulseRisk.Domain.Common;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Domain.Entities;

public sealed class TradingAccount
{
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
        Balance = balance;
        Leverage = leverage;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid ClientId { get; }

    public Money Balance { get; private set; }

    public CurrencyCode Currency => Balance.Currency;

    public decimal Leverage { get; }

    public DateTimeOffset CreatedAt { get; }

    public void UpdateBalance(Money balance)
    {
        if (balance.Currency != Currency)
        {
            throw new InvalidOperationException("Trading account currency cannot be changed.");
        }

        Balance = balance;
    }
}

