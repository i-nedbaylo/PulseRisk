using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PulseRisk.Application.Events;
using PulseRisk.Application.Repositories;
using PulseRisk.Infrastructure.Persistence;
using PulseRisk.IntegrationTests.TestInfrastructure;

namespace PulseRisk.IntegrationTests.Persistence;

public sealed class QuoteBatchWriterPostgreSqlTests
{
    [SkippableFact]
    public async Task WriteAsync_ShouldPersistQuoteBatch()
    {
        PostgreSqlTestContainer.SkipIfUnavailable();

        await using var postgres = PostgreSqlTestContainer.Create();
        await PostgreSqlTestContainer.StartOrSkipAsync(postgres);

        await using var application = new PulseRiskPostgresApplicationFactory(postgres.GetConnectionString());
        await using (var migrationScope = application.Services.CreateAsyncScope())
        {
            var dbContext = migrationScope.ServiceProvider.GetRequiredService<PulseRiskDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        var timestamp = new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);
        await using (var writerScope = application.Services.CreateAsyncScope())
        {
            var writer = writerScope.ServiceProvider.GetRequiredService<IQuoteBatchWriter>();

            await writer.WriteAsync([
                new QuoteTick("EURUSD", Bid: 1.10m, Ask: 1.11m, timestamp),
                new QuoteTick("GBPUSD", Bid: 1.27m, Ask: 1.28m, timestamp.AddMilliseconds(100))
            ], CancellationToken.None);
        }

        await using var assertionScope = application.Services.CreateAsyncScope();
        var assertionContext = assertionScope.ServiceProvider.GetRequiredService<PulseRiskDbContext>();
        var quotes = await assertionContext.Quotes
            .AsNoTracking()
            .ToArrayAsync();
        quotes = quotes
            .OrderBy(quote => quote.Symbol.Value, StringComparer.Ordinal)
            .ToArray();

        quotes.Should().HaveCount(2);
        quotes.Select(quote => quote.Symbol.Value)
            .Should()
            .Equal("EURUSD", "GBPUSD");
        quotes.Select(quote => quote.Bid.Value)
            .Should()
            .Equal(1.10m, 1.27m);
        quotes.Select(quote => quote.Ask.Value)
            .Should()
            .Equal(1.11m, 1.28m);
    }
}
