using PulseRisk.Domain.Entities;
using PulseRisk.Domain.ValueObjects;
using PulseRisk.Application.Positions;

namespace PulseRisk.Application.Repositories;

public interface IPositionRepository
{
    Task AddAsync(Position position, CancellationToken cancellationToken);

    Task<Position?> GetByTradingAccountAndSymbolAsync(
        Guid tradingAccountId,
        Symbol symbol,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Position>> ListOpenBySymbolAsync(
        Symbol symbol,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Position>> SearchAsync(
        GetPositionsQuery query,
        CancellationToken cancellationToken);
}
