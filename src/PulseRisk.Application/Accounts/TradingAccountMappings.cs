using PulseRisk.Domain.Entities;

namespace PulseRisk.Application.Accounts;

internal static class TradingAccountMappings
{
    public static TradingAccountDto ToDto(this TradingAccount account)
    {
        return new TradingAccountDto(
            account.Id,
            account.ClientId,
            account.Balance,
            account.Currency,
            account.Leverage,
            account.CreatedAt);
    }
}

