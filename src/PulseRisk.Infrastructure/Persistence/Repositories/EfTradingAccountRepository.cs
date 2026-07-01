using Microsoft.EntityFrameworkCore;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Entities;

namespace PulseRisk.Infrastructure.Persistence.Repositories;

internal sealed class EfTradingAccountRepository(PulseRiskDbContext dbContext) : ITradingAccountRepository
{
    public async Task AddAsync(TradingAccount account, CancellationToken cancellationToken)
    {
        await dbContext.TradingAccounts.AddAsync(account, cancellationToken);
    }

    public Task<TradingAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.TradingAccounts
            .FirstOrDefaultAsync(account => account.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TradingAccount>> ListByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken)
    {
        return await dbContext.TradingAccounts
            .AsNoTracking()
            .Where(account => account.ClientId == clientId)
            .OrderBy(account => account.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }
}

