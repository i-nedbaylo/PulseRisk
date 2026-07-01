using PulseRisk.Domain.Common;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Domain.Entities;

public sealed class RiskAlert
{
    private RiskAlert()
    {
        Message = string.Empty;
    }

    public RiskAlert(
        Guid id,
        Guid clientId,
        Guid tradingAccountId,
        Symbol symbol,
        RiskAlertType alertType,
        RiskSeverity severity,
        string message,
        DateTimeOffset createdAt,
        DateTimeOffset? resolvedAt)
    {
        DomainValidation.EnsureNotEmpty(id, nameof(id));
        DomainValidation.EnsureNotEmpty(clientId, nameof(clientId));
        DomainValidation.EnsureNotEmpty(tradingAccountId, nameof(tradingAccountId));

        Id = id;
        ClientId = clientId;
        TradingAccountId = tradingAccountId;
        Symbol = symbol;
        AlertType = alertType;
        Severity = severity;
        Message = DomainValidation.EnsureNotBlank(message, nameof(message));
        CreatedAt = createdAt;
        ResolvedAt = resolvedAt;
    }

    public Guid Id { get; private set; }

    public Guid ClientId { get; private set; }

    public Guid TradingAccountId { get; private set; }

    public Symbol Symbol { get; private set; }

    public RiskAlertType AlertType { get; private set; }

    public RiskSeverity Severity { get; private set; }

    public string Message { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public bool IsActive => ResolvedAt is null;

    public void Resolve(DateTimeOffset resolvedAt)
    {
        if (resolvedAt < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(resolvedAt), "Resolved timestamp must not be earlier than creation timestamp.");
        }

        ResolvedAt = resolvedAt;
    }
}
