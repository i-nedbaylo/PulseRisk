using Microsoft.Extensions.Logging;
using PulseRisk.Application.Events;
using PulseRisk.Application.Risk;

namespace PulseRisk.BackgroundWorkers.RiskEvents;

public sealed class RiskEvaluationProcessor(
    RiskEvaluationContextBuilder contextBuilder,
    ILogger<RiskEvaluationProcessor> logger)
{
    public async ValueTask ProcessAsync(
        RiskEvaluationRequested request,
        CancellationToken cancellationToken)
    {
        var preparation = await contextBuilder.BuildAsync(request, cancellationToken);
        if (!preparation.CanEvaluate)
        {
            logger.LogInformation(
                "Risk evaluation skipped for client {ClientId}, account {TradingAccountId}, symbol {Symbol}. Reason: {SkipReason}.",
                request.ClientId,
                request.TradingAccountId,
                request.Symbol,
                preparation.SkipReason);

            return;
        }

        logger.LogInformation(
            "Risk evaluation prepared for client {ClientId}, account {TradingAccountId}, symbol {Symbol}. Active rules {RuleCount}, exposure {NetExposure}, floating PnL {FloatingPnL}, margin level {MarginLevel}.",
            request.ClientId,
            request.TradingAccountId,
            request.Symbol,
            preparation.ActiveRules.Count,
            preparation.Metrics!.NetExposure,
            preparation.Metrics.FloatingPnL,
            preparation.Metrics.MarginLevel);
    }
}
