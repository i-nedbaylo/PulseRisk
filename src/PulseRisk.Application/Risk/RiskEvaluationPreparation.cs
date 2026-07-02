using PulseRisk.Application.Events;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Risk;

namespace PulseRisk.Application.Risk;

public sealed record RiskEvaluationPreparation(
    RiskEvaluationRequested Request,
    RiskMetricSnapshot? Metrics,
    RiskEvaluationContext? Context,
    IReadOnlyCollection<RiskRule> ActiveRules,
    string? SkipReason)
{
    public bool CanEvaluate => Metrics is not null && Context is not null && SkipReason is null;

    public static RiskEvaluationPreparation Ready(
        RiskEvaluationRequested request,
        RiskMetricSnapshot metrics,
        RiskEvaluationContext context,
        IReadOnlyCollection<RiskRule> activeRules)
    {
        return new RiskEvaluationPreparation(request, metrics, context, activeRules, SkipReason: null);
    }

    public static RiskEvaluationPreparation Skipped(
        RiskEvaluationRequested request,
        string reason)
    {
        return new RiskEvaluationPreparation(
            request,
            Metrics: null,
            Context: null,
            ActiveRules: [],
            reason);
    }
}
