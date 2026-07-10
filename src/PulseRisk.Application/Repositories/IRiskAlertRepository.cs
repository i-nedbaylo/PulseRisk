using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.ValueObjects;
using PulseRisk.Application.Common;
using PulseRisk.Application.Risk;

namespace PulseRisk.Application.Repositories;

public interface IRiskAlertRepository
{
    Task<bool> ExistsActiveAsync(
        Guid clientId,
        Guid tradingAccountId,
        Symbol symbol,
        RiskAlertType alertType,
        CancellationToken cancellationToken);

    Task<long> CountActiveAsync(CancellationToken cancellationToken);

    Task AddAsync(RiskAlert alert, CancellationToken cancellationToken);

    Task<PagedResult<RiskAlert>> SearchAsync(
        GetRiskAlertsQuery query,
        CancellationToken cancellationToken);
}
