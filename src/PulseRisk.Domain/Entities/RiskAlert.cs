using PulseRisk.Domain.Common;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Domain.Entities;

public sealed class RiskAlert
{
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

    public Guid Id { get; }

    public Guid ClientId { get; }

    public Guid TradingAccountId { get; }

    public Symbol Symbol { get; }

    public RiskAlertType AlertType { get; }

    public RiskSeverity Severity { get; }

    public string Message { get; }

    public DateTimeOffset CreatedAt { get; }

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

