using PulseRisk.Domain.Common;

namespace PulseRisk.Domain.ValueObjects;

public readonly record struct Symbol
{
    public Symbol(string value)
    {
        Value = DomainValidation.EnsureNotBlank(value, nameof(value)).ToUpperInvariant();
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }
}

