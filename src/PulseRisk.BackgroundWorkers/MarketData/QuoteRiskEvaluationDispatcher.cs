using Microsoft.Extensions.Logging;
using PulseRisk.Application.Events;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.BackgroundWorkers.MarketData;

public sealed class QuoteRiskEvaluationDispatcher(
    IPositionRepository positions,
    IEventWriter<RiskEvaluationRequested> riskEvaluationRequests,
    ILogger<QuoteRiskEvaluationDispatcher> logger)
{
    public async Task<int> PublishAsync(
        IReadOnlyCollection<QuoteTick> ticks,
        CancellationToken cancellationToken)
    {
        if (ticks.Count == 0)
        {
            return 0;
        }

        var latestTicks = ticks
            .Select(tick => new QuoteRiskTick(new Symbol(tick.Symbol), tick))
            .GroupBy(item => item.Symbol)
            .Select(group => group.OrderByDescending(item => item.Tick.Timestamp).First())
            .OrderBy(item => item.Symbol.Value)
            .ToArray();

        var publishedCount = 0;

        foreach (var latestTick in latestTicks)
        {
            var openPositions = await positions.ListOpenBySymbolAsync(
                latestTick.Symbol,
                cancellationToken);

            foreach (var position in openPositions)
            {
                await riskEvaluationRequests.WriteAsync(
                    new RiskEvaluationRequested(
                        position.ClientId,
                        position.TradingAccountId,
                        latestTick.Symbol.Value,
                        TradeId: null,
                        PositionId: position.Id,
                        Reason: "QuoteTick",
                        RequestedAt: latestTick.Tick.Timestamp),
                    cancellationToken);

                publishedCount++;
            }
        }

        logger.LogInformation(
            "Quote-driven risk dispatcher published {RiskRequestCount} risk evaluation requests for {SymbolCount} symbols.",
            publishedCount,
            latestTicks.Length);

        return publishedCount;
    }

    private sealed record QuoteRiskTick(Symbol Symbol, QuoteTick Tick);
}
