using PulseRisk.Domain.Entities;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Application.Repositories;

public interface IInstrumentRepository
{
    Task AddAsync(Instrument instrument, CancellationToken cancellationToken);

    Task<Instrument?> GetBySymbolAsync(Symbol symbol, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Instrument>> ListAsync(CancellationToken cancellationToken);
}

