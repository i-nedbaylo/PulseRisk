using PulseRisk.Domain.Enums;
using PulseRisk.Domain.Risk;

namespace PulseRisk.Application.Risk;

public sealed class RiskRuleStrategyResolver
{
    private readonly IReadOnlyDictionary<RiskRuleType, IRiskRuleStrategy> _strategies;

    public RiskRuleStrategyResolver(IEnumerable<IRiskRuleStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(strategy => strategy.RuleType);
    }

    public bool TryResolve(RiskRuleType ruleType, out IRiskRuleStrategy strategy)
    {
        return _strategies.TryGetValue(ruleType, out strategy!);
    }
}
