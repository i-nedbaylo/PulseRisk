using Microsoft.EntityFrameworkCore;
using PulseRisk.Application.Repositories;
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

    public async Task AddAsync(RiskAlert alert, CancellationToken cancellationToken)
    {
        await dbContext.RiskAlerts.AddAsync(alert, cancellationToken);
    }
}
