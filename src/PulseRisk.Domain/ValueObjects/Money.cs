using PulseRisk.Domain.Enums;

namespace PulseRisk.Domain.ValueObjects;

public readonly record struct Money
{
    public Money(decimal amount, CurrencyCode currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public CurrencyCode Currency { get; }

    public override string ToString()
    {
        return $"{Amount} {Currency}";
    }
}

