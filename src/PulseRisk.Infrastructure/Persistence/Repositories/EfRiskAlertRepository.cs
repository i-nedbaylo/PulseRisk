using Microsoft.EntityFrameworkCore;
using PulseRisk.Application.Common;
using PulseRisk.Application.Repositories;
using PulseRisk.Application.Risk;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Infrastructure.Persistence.Repositories;

internal sealed class EfRiskAlertRepository(PulseRiskDbContext dbContext) : IRiskAlertRepository
{
    public Task<bool> ExistsActiveAsync(
        Guid clientId,
        Guid tradingAccountId,
        Symbol symbol,
        RiskAlertType alertType,
        CancellationToken cancellationToken)
    {
        return dbContext.RiskAlerts
            .AsNoTracking()
            .AnyAsync(
                alert =>
                    alert.ClientId == clientId
                    && alert.TradingAccountId == tradingAccountId
                    && alert.Symbol == symbol
                    && alert.AlertType == alertType
                    && alert.ResolvedAt == null,
                cancellationToken);
    }

    public Task<long> CountActiveAsync(CancellationToken cancellationToken)
    {
        return dbContext.RiskAlerts
            .AsNoTracking()
            .LongCountAsync(alert => alert.ResolvedAt == null, cancellationToken);
    }

    public async Task AddAsync(RiskAlert alert, CancellationToken cancellationToken)
    {
        await dbContext.RiskAlerts.AddAsync(alert, cancellationToken);
    }

    public async Task<PagedResult<RiskAlert>> SearchAsync(
        GetRiskAlertsQuery query,
        CancellationToken cancellationToken)
    {
        var source = dbContext.RiskAlerts.AsNoTracking();

        if (query.ClientId is { } clientId)
        {
            source = source.Where(alert => alert.ClientId == clientId);
        }

        if (query.TradingAccountId is { } tradingAccountId)
        {
            source = source.Where(alert => alert.TradingAccountId == tradingAccountId);
        }

        if (query.Symbol is { } symbol)
        {
            source = source.Where(alert => alert.Symbol == symbol);
        }

        if (query.AlertType is { } alertType)
        {
            source = source.Where(alert => alert.AlertType == alertType);
        }

        if (query.Severity is { } severity)
        {
            source = source.Where(alert => alert.Severity == severity);
        }

        if (query.ActiveOnly)
        {
            source = source.Where(alert => alert.ResolvedAt == null);
        }

        if (query.CreatedFrom is { } createdFrom)
        {
            source = source.Where(alert => alert.CreatedAt >= createdFrom);
        }

        if (query.CreatedTo is { } createdTo)
        {
            source = source.Where(alert => alert.CreatedAt <= createdTo);
        }

        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .OrderByDescending(alert => alert.CreatedAt)
            .ThenByDescending(alert => alert.Id)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<RiskAlert>(
            items,
            query.PageNumber,
            query.PageSize,
            totalCount);
    }
}
