using Microsoft.EntityFrameworkCore;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Infrastructure.Persistence.Repositories;

internal sealed class EfPositionRepository(PulseRiskDbContext dbContext) : IPositionRepository
{
    public async Task AddAsync(Position position, CancellationToken cancellationToken)
    {
        await dbContext.Positions.AddAsync(position, cancellationToken);
    }

    public Task<Position?> GetByTradingAccountAndSymbolAsync(
        Guid tradingAccountId,
        Symbol symbol,
        CancellationToken cancellationToken)
    {
        return dbContext.Positions
            .FirstOrDefaultAsync(
                position => position.TradingAccountId == tradingAccountId && position.Symbol == symbol,
                cancellationToken);
    }
}
