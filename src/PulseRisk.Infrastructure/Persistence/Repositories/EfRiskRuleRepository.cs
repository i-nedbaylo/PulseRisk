using Microsoft.EntityFrameworkCore;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Entities;

namespace PulseRisk.Infrastructure.Persistence.Repositories;

internal sealed class EfRiskRuleRepository(PulseRiskDbContext dbContext) : IRiskRuleRepository
{
    public async Task<IReadOnlyCollection<RiskRule>> ListEnabledAsync(CancellationToken cancellationToken)
    {
        return await dbContext.RiskRules
            .AsNoTracking()
            .Where(rule => rule.IsEnabled)
            .OrderBy(rule => rule.RuleType)
            .ThenBy(rule => rule.Id)
            .ToArrayAsync(cancellationToken);
    }
}
