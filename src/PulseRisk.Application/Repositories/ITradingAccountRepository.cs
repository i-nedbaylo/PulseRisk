using PulseRisk.Domain.Entities;

namespace PulseRisk.Application.Repositories;

public interface ITradingAccountRepository
{
    Task AddAsync(TradingAccount account, CancellationToken cancellationToken);

    Task<TradingAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TradingAccount>> ListByClientIdAsync(
        Guid clientId,
        CancellationToken cancellationToken);
}

