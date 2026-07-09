namespace PulseRisk.Application.Quotes;

public sealed record LatestQuoteDto(
    Guid Id,
    string Symbol,
    decimal Bid,
    decimal Ask,
    DateTimeOffset Timestamp);
