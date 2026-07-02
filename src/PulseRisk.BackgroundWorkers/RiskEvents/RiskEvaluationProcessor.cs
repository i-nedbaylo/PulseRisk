using Microsoft.Extensions.Logging;
using PulseRisk.Application.Common;
using PulseRisk.Application.Events;
using PulseRisk.Application.Repositories;
using PulseRisk.Application.Risk;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.Risk;

namespace PulseRisk.BackgroundWorkers.RiskEvents;

public sealed class RiskEvaluationProcessor(
    RiskEvaluationContextBuilder contextBuilder,
    RiskRuleStrategyResolver strategyResolver,
    RiskAlertFactory riskAlertFactory,
    IRiskAlertRepository riskAlerts,
    IUnitOfWork unitOfWork,
    IEventWriter<RiskAlertRaisedEvent> riskAlertEvents,
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

        var context = preparation.Context!;
        var createdAlerts = new List<RiskAlert>();
        var evaluationAlertKeys = new HashSet<(Guid ClientId, Guid TradingAccountId, string Symbol, RiskAlertType AlertType)>();
        var triggeredRules = 0;
        var suppressedAlerts = 0;
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

            var result = await strategy.EvaluateAsync(context, rule, cancellationToken);
            if (result.IsTriggered)
            {
                triggeredRules++;

                logger.LogWarning(
                    "Risk rule {RuleType} triggered for client {ClientId}, account {TradingAccountId}, symbol {Symbol}: {Message}.",
                    rule.RuleType,
                    request.ClientId,
                    request.TradingAccountId,
                    request.Symbol,
                    result.Message);

                var alert = riskAlertFactory.Create(context, result, request.RequestedAt);
                var alertKey = (
                    alert.ClientId,
                    alert.TradingAccountId,
                    alert.Symbol.Value,
                    alert.AlertType);

                if (!evaluationAlertKeys.Add(alertKey))
                {
                    suppressedAlerts++;

                    logger.LogInformation(
                        "Risk alert {AlertType} for client {ClientId}, account {TradingAccountId}, symbol {Symbol} was suppressed because it was already created in current evaluation.",
                        alert.AlertType,
                        alert.ClientId,
                        alert.TradingAccountId,
                        alert.Symbol.Value);

                    continue;
                }

                var activeAlertExists = await riskAlerts.ExistsActiveAsync(
                    alert.ClientId,
                    alert.TradingAccountId,
                    alert.Symbol,
                    alert.AlertType,
                    cancellationToken);

                if (activeAlertExists)
                {
                    suppressedAlerts++;

                    logger.LogInformation(
                        "Risk alert {AlertType} for client {ClientId}, account {TradingAccountId}, symbol {Symbol} was suppressed because an active alert already exists.",
                        alert.AlertType,
                        alert.ClientId,
                        alert.TradingAccountId,
                        alert.Symbol.Value);

                    continue;
                }

                await riskAlerts.AddAsync(alert, cancellationToken);
                createdAlerts.Add(alert);
            }
        }

        if (createdAlerts.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var alert in createdAlerts)
            {
                logger.LogWarning(
                    "Risk alert {AlertId} created for client {ClientId}, account {TradingAccountId}, symbol {Symbol}, type {AlertType}, severity {Severity}: {Message}.",
                    alert.Id,
                    alert.ClientId,
                    alert.TradingAccountId,
                    alert.Symbol.Value,
                    alert.AlertType,
                    alert.Severity,
                    alert.Message);

                await riskAlertEvents.WriteAsync(
                    new RiskAlertRaisedEvent(
                        alert.Id,
                        alert.ClientId,
                        alert.TradingAccountId,
                        alert.Symbol.Value,
                        alert.AlertType,
                        alert.Severity,
                        alert.Message,
                        alert.CreatedAt),
                    cancellationToken);
            }
        }

        logger.LogInformation(
            "Risk evaluation completed for client {ClientId}, account {TradingAccountId}, symbol {Symbol}. Active rules {RuleCount}, triggered rules {TriggeredRuleCount}, skipped rules {SkippedRuleCount}, created alerts {CreatedAlertCount}, suppressed alerts {SuppressedAlertCount}, exposure {NetExposure}, floating PnL {FloatingPnL}, margin level {MarginLevel}.",
            request.ClientId,
            request.TradingAccountId,
            request.Symbol,
            preparation.ActiveRules.Count,
            triggeredRules,
            skippedRules,
            createdAlerts.Count,
            suppressedAlerts,
            preparation.Metrics!.NetExposure,
            preparation.Metrics.FloatingPnL,
            preparation.Metrics.MarginLevel);
    }
}
