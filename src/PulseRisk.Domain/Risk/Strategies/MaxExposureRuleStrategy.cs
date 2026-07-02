using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;

namespace PulseRisk.Domain.Risk.Strategies;

public sealed class MaxExposureRuleStrategy : IRiskRuleStrategy
{
    public RiskRuleType RuleType => RiskRuleType.MaxExposureLimit;

    public ValueTask<RiskRuleEvaluationResult> EvaluateAsync(
        RiskEvaluationContext context,
        RiskRule rule,
        CancellationToken cancellationToken)
    {
        if (context.NetExposure <= rule.ThresholdValue)
        {
            return ValueTask.FromResult(RiskRuleEvaluationResult.NotTriggered());
        }

        return ValueTask.FromResult(RiskRuleEvaluationResult.Triggered(
            RiskAlertType.ExposureLimitExceeded,
            rule.Severity,
            $"Net exposure {context.NetExposure:F2} exceeds threshold {rule.ThresholdValue:F2} for {context.Symbol.Value}."));
    }
}
