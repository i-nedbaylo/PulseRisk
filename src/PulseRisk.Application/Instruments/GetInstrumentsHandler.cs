using PulseRisk.Application.Repositories;

namespace PulseRisk.Application.Instruments;

public sealed class GetInstrumentsHandler(IInstrumentRepository instruments)
{
    public async Task<IReadOnlyCollection<InstrumentDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var result = await instruments.ListAsync(cancellationToken);

        return result.Select(instrument => instrument.ToDto()).ToArray();
    }
}

