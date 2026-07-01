namespace PulseRisk.Application.Instruments;

public sealed record CreateInstrumentCommand(
    string Symbol,
    string BaseAsset,
    string QuoteAsset,
    int Digits,
    decimal ContractSize,
    bool IsActive = true);

