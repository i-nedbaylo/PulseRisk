using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;

namespace PulseRisk.Domain.Risk;

public interface IRiskRuleStrategy
{
    RiskRuleType RuleType { get; }

    ValueTask<RiskRuleEvaluationResult> EvaluateAsync(
        RiskEvaluationContext context,
        RiskRule rule,
        CancellationToken cancellationToken);
}
