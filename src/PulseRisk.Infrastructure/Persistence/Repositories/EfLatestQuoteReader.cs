using Microsoft.EntityFrameworkCore;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Infrastructure.Persistence.Repositories;

internal sealed class EfLatestQuoteReader(PulseRiskDbContext dbContext) : ILatestQuoteReader
{
    public Task<Quote?> GetLatestAsync(Symbol symbol, CancellationToken cancellationToken)
    {
        return dbContext.Quotes
            .AsNoTracking()
            .Where(quote => quote.Symbol == symbol)
            .OrderByDescending(quote => quote.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
