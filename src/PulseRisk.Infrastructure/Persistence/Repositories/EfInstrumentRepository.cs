using Microsoft.EntityFrameworkCore;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Infrastructure.Persistence.Repositories;

internal sealed class EfInstrumentRepository(PulseRiskDbContext dbContext) : IInstrumentRepository
{
    public async Task AddAsync(Instrument instrument, CancellationToken cancellationToken)
    {
        await dbContext.Instruments.AddAsync(instrument, cancellationToken);
    }

    public Task<Instrument?> GetBySymbolAsync(Symbol symbol, CancellationToken cancellationToken)
    {
        return dbContext.Instruments
            .FirstOrDefaultAsync(instrument => instrument.Symbol == symbol, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Instrument>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Instruments
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);
    }
}

