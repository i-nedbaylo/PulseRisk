using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;

namespace PulseRisk.Domain.Risk.Strategies;

public sealed class MaxLossRuleStrategy : IRiskRuleStrategy
{
    public RiskRuleType RuleType => RiskRuleType.MaxLossLimit;

    public ValueTask<RiskRuleEvaluationResult> EvaluateAsync(
        RiskEvaluationContext context,
        RiskRule rule,
        CancellationToken cancellationToken)
    {
        if (context.FloatingPnL > rule.ThresholdValue)
        {
            return ValueTask.FromResult(RiskRuleEvaluationResult.NotTriggered());
        }

        return ValueTask.FromResult(RiskRuleEvaluationResult.Triggered(
            RiskAlertType.MaxLossExceeded,
            rule.Severity,
            $"Floating PnL {context.FloatingPnL:F2} is at or below loss threshold {rule.ThresholdValue:F2} for {context.Symbol.Value}."));
    }
}
