using PulseRisk.Domain.Entities;

namespace PulseRisk.Domain.Risk;

public sealed class RiskAlertFactory
{
    public RiskAlert Create(
        RiskEvaluationContext context,
        RiskRuleEvaluationResult result,
        DateTimeOffset createdAt)
    {
        if (!result.IsTriggered)
        {
            throw new ArgumentException("Only triggered rule results can create risk alerts.", nameof(result));
        }

        if (result.AlertType is null)
        {
            throw new ArgumentException("Triggered rule result must contain alert type.", nameof(result));
        }

        if (result.Severity is null)
        {
            throw new ArgumentException("Triggered rule result must contain severity.", nameof(result));
        }

        if (string.IsNullOrWhiteSpace(result.Message))
        {
            throw new ArgumentException("Triggered rule result must contain alert message.", nameof(result));
        }

        return new RiskAlert(
            Guid.NewGuid(),
            context.ClientId,
            context.TradingAccountId,
            context.Symbol,
            result.AlertType.Value,
            result.Severity.Value,
            result.Message,
            createdAt,
            resolvedAt: null);
    }
}
