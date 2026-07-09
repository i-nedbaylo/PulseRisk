using Microsoft.EntityFrameworkCore;
using PulseRisk.Application.Common;
using PulseRisk.Application.Repositories;
using PulseRisk.Application.Trades;
using PulseRisk.Domain.Entities;

namespace PulseRisk.Infrastructure.Persistence.Repositories;

internal sealed class EfTradeRepository(PulseRiskDbContext dbContext) : ITradeRepository
{
    public async Task AddAsync(Trade trade, CancellationToken cancellationToken)
    {
        await dbContext.Trades.AddAsync(trade, cancellationToken);
    }

    public Task<Trade?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Trades
            .AsNoTracking()
            .FirstOrDefaultAsync(trade => trade.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Trade>> SearchAsync(
        GetTradesQuery query,
        CancellationToken cancellationToken)
    {
        var source = dbContext.Trades.AsNoTracking();

        if (query.ClientId is { } clientId)
        {
            source = source.Where(trade => trade.ClientId == clientId);
        }

        if (query.TradingAccountId is { } tradingAccountId)
        {
            source = source.Where(trade => trade.TradingAccountId == tradingAccountId);
        }

        if (query.Symbol is { } symbol)
        {
            source = source.Where(trade => trade.Symbol == symbol);
        }

        if (query.Side is { } side)
        {
            source = source.Where(trade => trade.Side == side);
        }

        if (query.CreatedFrom is { } createdFrom)
        {
            source = source.Where(trade => trade.CreatedAt >= createdFrom);
        }

        if (query.CreatedTo is { } createdTo)
        {
            source = source.Where(trade => trade.CreatedAt <= createdTo);
        }

        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .OrderByDescending(trade => trade.CreatedAt)
            .ThenByDescending(trade => trade.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<Trade>(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);
    }
}
