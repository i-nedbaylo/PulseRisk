using PulseRisk.Application.Common;

namespace PulseRisk.Infrastructure.Persistence;

internal sealed class EfUnitOfWork(PulseRiskDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}

