using PulseRisk.Application.Common;
using PulseRisk.Application.Repositories;

namespace PulseRisk.Application.Trades;

public sealed class GetTradesHandler(ITradeRepository trades)
{
    public async Task<PagedResult<TradeDto>> HandleAsync(
        GetTradesQuery query,
        CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = Pagination.Normalize(query.PageNumber, query.PageSize);
        var normalizedQuery = query with
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await trades.SearchAsync(normalizedQuery, cancellationToken);

        return new PagedResult<TradeDto>(
            result.Items.Select(trade => trade.ToDto()).ToArray(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount);
    }
}
