using PulseRisk.Domain.Enums;

namespace PulseRisk.Application.Accounts;

public sealed record CreateTradingAccountCommand(
    Guid ClientId,
    decimal Balance,
    CurrencyCode Currency,
    decimal Leverage);

