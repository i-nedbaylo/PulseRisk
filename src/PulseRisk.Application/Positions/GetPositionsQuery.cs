using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Application.Positions;

public sealed record GetPositionsQuery(
    Guid? ClientId = null,
    Guid? TradingAccountId = null,
    Symbol? Symbol = null,
    bool OpenOnly = false);
