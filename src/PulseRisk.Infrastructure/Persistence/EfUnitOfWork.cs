using PulseRisk.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace PulseRisk.Infrastructure.Persistence;

internal sealed class EfUnitOfWork(PulseRiskDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "A concurrent position update was detected.",
                exception);
        }
    }

    public void ClearChanges()
    {
        dbContext.ChangeTracker.Clear();
    }
}
