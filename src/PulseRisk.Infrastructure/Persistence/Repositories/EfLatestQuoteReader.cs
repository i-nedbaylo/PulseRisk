using Dapper;
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

    public async Task<IReadOnlyCollection<Quote>> ListLatestAsync(
        Symbol? symbol,
        CancellationToken cancellationToken)
    {
        if (symbol is { } requestedSymbol)
        {
            var quote = await GetLatestAsync(requestedSymbol, cancellationToken);
            return quote is null
                ? []
                : [quote];
        }

        var connection = dbContext.Database.GetDbConnection();
        var rows = await connection.QueryAsync<LatestQuoteRow>(
            new CommandDefinition(
                """
                SELECT DISTINCT ON (symbol)
                    id,
                    symbol,
                    bid,
                    ask,
                    "timestamp"
                FROM quotes
                ORDER BY symbol, "timestamp" DESC
                """,
                cancellationToken: cancellationToken));

        return rows
            .Select(row => new Quote(
                row.Id,
                new Symbol(row.Symbol),
                new Price(row.Bid),
                new Price(row.Ask),
                row.Timestamp))
            .ToArray();
    }

    private sealed class LatestQuoteRow
    {
        public Guid Id { get; init; }

        public string Symbol { get; init; } = string.Empty;

        public decimal Bid { get; init; }

        public decimal Ask { get; init; }

        public DateTimeOffset Timestamp { get; init; }
    }
}
