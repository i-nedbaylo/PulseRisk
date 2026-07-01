using PulseRisk.Domain.Entities;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Application.Repositories;

public interface IPositionRepository
{
    Task AddAsync(Position position, CancellationToken cancellationToken);

    Task<Position?> GetByTradingAccountAndSymbolAsync(
        Guid tradingAccountId,
        Symbol symbol,
        CancellationToken cancellationToken);
}
