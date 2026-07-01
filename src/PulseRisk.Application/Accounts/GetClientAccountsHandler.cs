using PulseRisk.Application.Repositories;

namespace PulseRisk.Application.Accounts;

public sealed class GetClientAccountsHandler(ITradingAccountRepository accounts)
{
    public async Task<IReadOnlyCollection<TradingAccountDto>> HandleAsync(
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var result = await accounts.ListByClientIdAsync(clientId, cancellationToken);

        return result.Select(account => account.ToDto()).ToArray();
    }
}

