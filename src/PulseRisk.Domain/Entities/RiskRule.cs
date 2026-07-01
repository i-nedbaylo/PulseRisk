using PulseRisk.Domain.Common;
using PulseRisk.Domain.Enums;

namespace PulseRisk.Domain.Entities;

public sealed class RiskRule
{
    public RiskRule(
        Guid id,
        string name,
        RiskRuleType ruleType,
        decimal thresholdValue,
        RiskSeverity severity,
        bool isEnabled)
    {
        DomainValidation.EnsureNotEmpty(id, nameof(id));

        Id = id;
        Name = DomainValidation.EnsureNotBlank(name, nameof(name));
        RuleType = ruleType;
        ThresholdValue = thresholdValue;
        Severity = severity;
        IsEnabled = isEnabled;
    }

    public Guid Id { get; }

    public string Name { get; }

    public RiskRuleType RuleType { get; }

    public decimal ThresholdValue { get; private set; }

    public RiskSeverity Severity { get; private set; }

    public bool IsEnabled { get; private set; }

    public void UpdateThreshold(decimal thresholdValue)
    {
        ThresholdValue = thresholdValue;
    }

    public void Enable()
    {
        IsEnabled = true;
    }

    public void Disable()
    {
        IsEnabled = false;
    }
}

