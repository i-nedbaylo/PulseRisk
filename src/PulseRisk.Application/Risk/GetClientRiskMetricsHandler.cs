using PulseRisk.Application.Common;
using PulseRisk.Application.Positions;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Services;

namespace PulseRisk.Application.Risk;

public sealed class GetClientRiskMetricsHandler(
    IClientRepository clients,
    ITradingAccountRepository accounts,
    IPositionRepository positions,
    IInstrumentRepository instruments,
    ILatestQuoteReader latestQuotes)
{
    public async Task<IReadOnlyCollection<ClientRiskMetricDto>> HandleAsync(
        GetClientRiskMetricsQuery query,
        CancellationToken cancellationToken)
    {
        if (await clients.GetByIdAsync(query.ClientId, cancellationToken) is null)
        {
            throw new EntityNotFoundException("Client", query.ClientId.ToString());
        }

        var clientAccounts = await accounts.ListByClientIdAsync(query.ClientId, cancellationToken);
        var accountsById = clientAccounts.ToDictionary(account => account.Id);
        var instrumentsBySymbol = (await instruments.ListAsync(cancellationToken))
            .ToDictionary(instrument => instrument.Symbol);
        var latestQuotesBySymbol = (await latestQuotes.ListLatestAsync(null, cancellationToken))
            .ToDictionary(quote => quote.Symbol);

        var openPositions = await positions.SearchAsync(
            new GetPositionsQuery(ClientId: query.ClientId, OpenOnly: true),
            cancellationToken);

        var metrics = new List<ClientRiskMetricDto>(openPositions.Count);
        foreach (var position in openPositions)
        {
            if (!accountsById.TryGetValue(position.TradingAccountId, out var account))
            {
                continue;
            }

            if (!instrumentsBySymbol.TryGetValue(position.Symbol, out var instrument))
            {
                throw new EntityNotFoundException("Instrument", position.Symbol.Value);
            }

            latestQuotesBySymbol.TryGetValue(position.Symbol, out var latestQuote);
            var floatingPnL = latestQuote is null
                ? position.FloatingPnL
                : PnLCalculator.CalculateFloatingPnL(
                    position.NetVolume,
                    position.AveragePrice,
                    latestQuote.Bid,
                    latestQuote.Ask,
                    instrument.ContractSize);

            var netExposure = Math.Abs(position.NetVolume) * position.AveragePrice * instrument.ContractSize;
            var marginUsed = netExposure / account.Leverage;
            var equity = account.Balance + floatingPnL;
            var marginLevel = marginUsed == 0
                ? decimal.MaxValue
                : equity / marginUsed * 100m;

            metrics.Add(new ClientRiskMetricDto(
                position.ClientId,
                position.TradingAccountId,
                position.Symbol.Value,
                position.NetVolume,
                position.AveragePrice,
                netExposure,
                floatingPnL,
                equity,
                marginUsed,
                marginLevel,
                latestQuote?.Timestamp));
        }

        return metrics
            .OrderBy(metric => metric.TradingAccountId)
            .ThenBy(metric => metric.Symbol, StringComparer.Ordinal)
            .ToArray();
    }
}
