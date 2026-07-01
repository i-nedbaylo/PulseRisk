using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Domain.Entities;

public sealed class Quote
{
    public Quote(Guid id, Symbol symbol, Price bid, Price ask, DateTimeOffset timestamp)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier must not be empty.", nameof(id));
        }

        if (bid.Value > ask.Value)
        {
            throw new ArgumentException("Bid price must not be greater than ask price.", nameof(bid));
        }

        Id = id;
        Symbol = symbol;
        Bid = bid;
        Ask = ask;
        Timestamp = timestamp;
    }

    public Guid Id { get; }

    public Symbol Symbol { get; }

    public Price Bid { get; }

    public Price Ask { get; }

    public DateTimeOffset Timestamp { get; }
}

