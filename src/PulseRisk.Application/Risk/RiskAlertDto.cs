using PulseRisk.Domain.Enums;

namespace PulseRisk.Application.Risk;

public sealed record RiskAlertDto(
    Guid Id,
    Guid ClientId,
    Guid TradingAccountId,
    string Symbol,
    RiskAlertType AlertType,
    RiskSeverity Severity,
    string Message,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    bool IsActive);
