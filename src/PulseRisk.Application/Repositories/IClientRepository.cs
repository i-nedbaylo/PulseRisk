using PulseRisk.Domain.Entities;

namespace PulseRisk.Application.Repositories;

public interface IClientRepository
{
    Task AddAsync(Client client, CancellationToken cancellationToken);

    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Client>> ListAsync(CancellationToken cancellationToken);
}

