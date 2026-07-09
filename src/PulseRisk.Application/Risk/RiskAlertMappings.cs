using PulseRisk.Domain.Entities;

namespace PulseRisk.Application.Risk;

internal static class RiskAlertMappings
{
    public static RiskAlertDto ToDto(this RiskAlert alert)
    {
        return new RiskAlertDto(
            alert.Id,
            alert.ClientId,
            alert.TradingAccountId,
            alert.Symbol.Value,
            alert.AlertType,
            alert.Severity,
            alert.Message,
            alert.CreatedAt,
            alert.ResolvedAt,
            alert.IsActive);
    }
}
