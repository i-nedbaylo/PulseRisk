using PulseRisk.Domain.Enums;

namespace PulseRisk.Domain.Risk;

public sealed record RiskRuleEvaluationResult(
    bool IsTriggered,
    RiskAlertType? AlertType,
    RiskSeverity? Severity,
    string? Message)
{
    public static RiskRuleEvaluationResult NotTriggered()
    {
        return new RiskRuleEvaluationResult(false, null, null, null);
    }

    public static RiskRuleEvaluationResult Triggered(
        RiskAlertType alertType,
        RiskSeverity severity,
        string message)
    {
        return new RiskRuleEvaluationResult(true, alertType, severity, message);
    }
}

