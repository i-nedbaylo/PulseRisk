using PulseRisk.Domain.Entities;

namespace PulseRisk.Application.Repositories;

public interface ITradeRepository
{
    Task AddAsync(Trade trade, CancellationToken cancellationToken);

    Task<Trade?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
