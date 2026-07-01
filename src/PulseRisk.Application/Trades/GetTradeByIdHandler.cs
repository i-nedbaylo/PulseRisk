using PulseRisk.Application.Common;
using PulseRisk.Application.Repositories;

namespace PulseRisk.Application.Trades;

public sealed class GetTradeByIdHandler(ITradeRepository trades)
{
    public async Task<TradeDto> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var trade = await trades.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException("Trade", id.ToString());

        return trade.ToDto();
    }
}
