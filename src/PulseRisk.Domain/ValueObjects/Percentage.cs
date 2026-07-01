namespace PulseRisk.Domain.ValueObjects;

public readonly record struct Percentage
{
    public Percentage(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Percentage must not be negative.");
        }

        Value = value;
    }

    public decimal Value { get; }

    public override string ToString()
    {
        return $"{Value}%";
    }
}

