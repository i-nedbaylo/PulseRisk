using PulseRisk.Application.Common;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Application.Risk;

public sealed record GetRiskAlertsQuery(
    Guid? ClientId = null,
    Guid? TradingAccountId = null,
    Symbol? Symbol = null,
    RiskAlertType? AlertType = null,
    RiskSeverity? Severity = null,
    bool ActiveOnly = false,
    DateTimeOffset? CreatedFrom = null,
    DateTimeOffset? CreatedTo = null,
    int PageNumber = Pagination.DefaultPageNumber,
    int PageSize = Pagination.DefaultPageSize);
