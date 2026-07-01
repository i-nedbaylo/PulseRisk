using PulseRisk.Application.Common;
using PulseRisk.Application.Repositories;

namespace PulseRisk.Application.Accounts;

public sealed class GetTradingAccountByIdHandler(ITradingAccountRepository accounts)
{
    public async Task<TradingAccountDto> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException("TradingAccount", id.ToString());

        return account.ToDto();
    }
}

