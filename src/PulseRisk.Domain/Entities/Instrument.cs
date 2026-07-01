using PulseRisk.Domain.Common;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Domain.Entities;

public sealed class Instrument
{
    private Instrument()
    {
        BaseAsset = string.Empty;
        QuoteAsset = string.Empty;
    }

    public Instrument(
        Symbol symbol,
        string baseAsset,
        string quoteAsset,
        int digits,
        decimal contractSize,
        bool isActive)
    {
        if (digits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(digits), "Digits must not be negative.");
        }

        if (contractSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(contractSize), "Contract size must be greater than zero.");
        }

        Symbol = symbol;
        BaseAsset = DomainValidation.EnsureNotBlank(baseAsset, nameof(baseAsset)).ToUpperInvariant();
        QuoteAsset = DomainValidation.EnsureNotBlank(quoteAsset, nameof(quoteAsset)).ToUpperInvariant();
        Digits = digits;
        ContractSize = contractSize;
        IsActive = isActive;
    }

    public Symbol Symbol { get; private set; }

    public string BaseAsset { get; private set; }

    public string QuoteAsset { get; private set; }

    public int Digits { get; private set; }

    public decimal ContractSize { get; private set; }

    public bool IsActive { get; private set; }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
