using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;

namespace PulseRisk.Domain.Risk.Strategies;

public sealed class MarginLevelWarningRuleStrategy : IRiskRuleStrategy
{
    public RiskRuleType RuleType => RiskRuleType.MarginLevelWarning;

    public ValueTask<RiskRuleEvaluationResult> EvaluateAsync(
        RiskEvaluationContext context,
        RiskRule rule,
        CancellationToken cancellationToken)
    {
        if (context.MarginLevel > rule.ThresholdValue)
        {
            return ValueTask.FromResult(RiskRuleEvaluationResult.NotTriggered());
        }

        return ValueTask.FromResult(RiskRuleEvaluationResult.Triggered(
            RiskAlertType.MarginLevelWarning,
            rule.Severity,
            $"Margin level {context.MarginLevel:F2}% is at or below threshold {rule.ThresholdValue:F2}% for {context.Symbol.Value}."));
    }
}
