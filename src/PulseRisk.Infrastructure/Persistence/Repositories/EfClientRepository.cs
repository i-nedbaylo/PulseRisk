using Microsoft.EntityFrameworkCore;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Entities;

namespace PulseRisk.Infrastructure.Persistence.Repositories;

internal sealed class EfClientRepository(PulseRiskDbContext dbContext) : IClientRepository
{
    public async Task AddAsync(Client client, CancellationToken cancellationToken)
    {
        await dbContext.Clients.AddAsync(client, cancellationToken);
    }

    public Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Clients
            .FirstOrDefaultAsync(client => client.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Client>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Clients
            .AsNoTracking()
            .OrderBy(client => client.CreatedAt)
            .ThenBy(client => client.Name)
            .ToArrayAsync(cancellationToken);
    }
}

