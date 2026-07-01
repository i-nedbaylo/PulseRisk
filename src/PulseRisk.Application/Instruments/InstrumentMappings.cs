using PulseRisk.Domain.Entities;

namespace PulseRisk.Application.Instruments;

internal static class InstrumentMappings
{
    public static InstrumentDto ToDto(this Instrument instrument)
    {
        return new InstrumentDto(
            instrument.Symbol.Value,
            instrument.BaseAsset,
            instrument.QuoteAsset,
            instrument.Digits,
            instrument.ContractSize,
            instrument.IsActive);
    }
}

