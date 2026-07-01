using PulseRisk.Domain.Enums;

namespace PulseRisk.Application.Accounts;

public sealed record TradingAccountDto(
    Guid Id,
    Guid ClientId,
    decimal Balance,
    CurrencyCode Currency,
    decimal Leverage,
    DateTimeOffset CreatedAt);

