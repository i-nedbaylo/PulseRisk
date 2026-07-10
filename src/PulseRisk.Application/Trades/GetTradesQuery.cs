using PulseRisk.Application.Common;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Application.Trades;

public sealed record GetTradesQuery(
    Guid? ClientId = null,
    Guid? TradingAccountId = null,
    Symbol? Symbol = null,
    TradeSide? Side = null,
    DateTimeOffset? CreatedFrom = null,
    DateTimeOffset? CreatedTo = null,
    int PageNumber = Pagination.DefaultPageNumber,
    int PageSize = Pagination.DefaultPageSize);
