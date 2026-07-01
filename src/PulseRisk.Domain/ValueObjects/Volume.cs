namespace PulseRisk.Domain.ValueObjects;

public readonly record struct Volume
{
    public Volume(decimal value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Volume must be greater than zero.");
        }

        Value = value;
    }

    public decimal Value { get; }

    public override string ToString()
    {
        return Value.ToString();
    }
}

