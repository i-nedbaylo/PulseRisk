using PulseRisk.Domain.Entities;
using PulseRisk.Application.Common;
using PulseRisk.Application.Trades;

namespace PulseRisk.Application.Repositories;

public interface ITradeRepository
{
    Task AddAsync(Trade trade, CancellationToken cancellationToken);

    Task<Trade?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<Trade>> SearchAsync(
        GetTradesQuery query,
        CancellationToken cancellationToken);
}
