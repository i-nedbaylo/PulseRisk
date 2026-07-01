using Microsoft.EntityFrameworkCore;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Entities;

namespace PulseRisk.Infrastructure.Persistence.Repositories;

internal sealed class EfTradeRepository(PulseRiskDbContext dbContext) : ITradeRepository
{
    public async Task AddAsync(Trade trade, CancellationToken cancellationToken)
    {
        await dbContext.Trades.AddAsync(trade, cancellationToken);
    }

    public Task<Trade?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Trades
            .AsNoTracking()
            .FirstOrDefaultAsync(trade => trade.Id == id, cancellationToken);
    }
}
