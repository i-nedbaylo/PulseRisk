namespace PulseRisk.Application.Instruments;

public sealed record InstrumentDto(
    string Symbol,
    string BaseAsset,
    string QuoteAsset,
    int Digits,
    decimal ContractSize,
    bool IsActive);

