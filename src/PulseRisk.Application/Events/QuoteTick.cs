namespace PulseRisk.Application.Events;

public sealed record QuoteTick(
    string Symbol,
    decimal Bid,
    decimal Ask,
    DateTimeOffset Timestamp);
