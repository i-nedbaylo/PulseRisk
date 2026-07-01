using Microsoft.Extensions.Logging;
using PulseRisk.Application.Events;

namespace PulseRisk.BackgroundWorkers.RiskEvents;

public sealed class PositionChangedEventProcessor(
    IEventWriter<RiskEvaluationRequested> riskEvaluationRequests,
    ILogger<PositionChangedEventProcessor> logger)
{
    public async ValueTask ProcessAsync(
        PositionChangedEvent message,
        CancellationToken cancellationToken)
    {
        var request = new RiskEvaluationRequested(
            message.ClientId,
            message.TradingAccountId,
            message.Symbol,
            message.TradeId,
            message.PositionId,
            Reason: "PositionChanged",
            RequestedAt: message.OccurredAt);

        await riskEvaluationRequests.WriteAsync(request, cancellationToken);

        logger.LogInformation(
            "Risk evaluation requested for client {ClientId}, account {TradingAccountId}, symbol {Symbol}, trade {TradeId}.",
            message.ClientId,
            message.TradingAccountId,
            message.Symbol,
            message.TradeId);
    }
}
