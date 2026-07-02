using PulseRisk.Domain.Entities;

namespace PulseRisk.Application.Repositories;

public interface IRiskRuleRepository
{
    Task<IReadOnlyCollection<RiskRule>> ListEnabledAsync(CancellationToken cancellationToken);
}
