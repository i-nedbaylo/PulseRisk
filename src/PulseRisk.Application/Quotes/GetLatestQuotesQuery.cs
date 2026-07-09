using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Application.Quotes;

public sealed record GetLatestQuotesQuery(Symbol? Symbol = null);
