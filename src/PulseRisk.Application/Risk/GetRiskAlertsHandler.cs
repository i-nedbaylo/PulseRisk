using PulseRisk.Application.Common;
using PulseRisk.Application.Repositories;

namespace PulseRisk.Application.Risk;

public sealed class GetRiskAlertsHandler(IRiskAlertRepository alerts)
{
    public async Task<PagedResult<RiskAlertDto>> HandleAsync(
        GetRiskAlertsQuery query,
        CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = Pagination.Normalize(query.PageNumber, query.PageSize);
        var normalizedQuery = query with
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await alerts.SearchAsync(normalizedQuery, cancellationToken);

        return new PagedResult<RiskAlertDto>(
            result.Items.Select(alert => alert.ToDto()).ToArray(),
            result.PageNumber,
            result.PageSize,
            result.TotalCount);
    }
}
