namespace PulseRisk.Domain.ValueObjects;

public readonly record struct Price
{
    public Price(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Price must be greater than zero.");
        }

        Value = value;
    }

    public decimal Value { get; }

    public override string ToString()
    {
        return Value.ToString();
    }
}

