using Microsoft.Extensions.Logging;
using PulseRisk.Application.Events;
using PulseRisk.Application.Risk;
using PulseRisk.Domain.Risk;

namespace PulseRisk.BackgroundWorkers.RiskEvents;

public sealed class RiskEvaluationProcessor(
    RiskEvaluationContextBuilder contextBuilder,
    RiskRuleStrategyResolver strategyResolver,
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

        var triggeredResults = new List<RiskRuleEvaluationResult>();
        var skippedRules = 0;

        foreach (var rule in preparation.ActiveRules)
        {
            if (!strategyResolver.TryResolve(rule.RuleType, out var strategy))
            {
                skippedRules++;

                logger.LogInformation(
                    "Risk rule {RuleType} is active but no strategy is registered yet.",
                    rule.RuleType);

                continue;
            }

            var result = await strategy.EvaluateAsync(preparation.Context!, rule, cancellationToken);
            if (result.IsTriggered)
            {
                triggeredResults.Add(result);

                logger.LogWarning(
                    "Risk rule {RuleType} triggered for client {ClientId}, account {TradingAccountId}, symbol {Symbol}: {Message}.",
                    rule.RuleType,
                    request.ClientId,
                    request.TradingAccountId,
                    request.Symbol,
                    result.Message);
            }
        }

        logger.LogInformation(
            "Risk evaluation completed for client {ClientId}, account {TradingAccountId}, symbol {Symbol}. Active rules {RuleCount}, triggered rules {TriggeredRuleCount}, skipped rules {SkippedRuleCount}, exposure {NetExposure}, floating PnL {FloatingPnL}, margin level {MarginLevel}.",
            request.ClientId,
            request.TradingAccountId,
            request.Symbol,
            preparation.ActiveRules.Count,
            triggeredResults.Count,
            skippedRules,
            preparation.Metrics!.NetExposure,
            preparation.Metrics.FloatingPnL,
            preparation.Metrics.MarginLevel);
    }
}
