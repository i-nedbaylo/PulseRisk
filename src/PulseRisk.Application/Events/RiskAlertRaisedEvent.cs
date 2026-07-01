using PulseRisk.Domain.Enums;

namespace PulseRisk.Application.Events;

public sealed record RiskAlertRaisedEvent(
    Guid AlertId,
    Guid ClientId,
    Guid TradingAccountId,
    string Symbol,
    RiskAlertType AlertType,
    RiskSeverity Severity,
    string Message,
    DateTimeOffset OccurredAt);
