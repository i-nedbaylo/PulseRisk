using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Domain.Entities;

public sealed class Quote
{
    private Quote()
    {
    }

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

    public Guid Id { get; private set; }

    public Symbol Symbol { get; private set; }

    public Price Bid { get; private set; }

    public Price Ask { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }
}
