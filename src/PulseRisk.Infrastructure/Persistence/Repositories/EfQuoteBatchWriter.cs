using PulseRisk.Application.Events;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Infrastructure.Persistence.Repositories;

internal sealed class EfQuoteBatchWriter(PulseRiskDbContext dbContext) : IQuoteBatchWriter
{
    public async Task WriteAsync(
        IReadOnlyCollection<QuoteTick> ticks,
        CancellationToken cancellationToken)
    {
        if (ticks.Count == 0)
        {
            return;
        }

        var quotes = ticks
            .Select(tick => new Quote(
                Guid.NewGuid(),
                new Symbol(tick.Symbol),
                new Price(tick.Bid),
                new Price(tick.Ask),
                tick.Timestamp))
            .ToArray();

        await dbContext.Quotes.AddRangeAsync(quotes, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.ChangeTracker.Clear();
    }
}
