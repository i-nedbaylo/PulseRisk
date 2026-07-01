using PulseRisk.Domain.Common;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Domain.Entities;

public sealed class Instrument
{
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

    public Symbol Symbol { get; }

    public string BaseAsset { get; }

    public string QuoteAsset { get; }

    public int Digits { get; }

    public decimal ContractSize { get; }

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

