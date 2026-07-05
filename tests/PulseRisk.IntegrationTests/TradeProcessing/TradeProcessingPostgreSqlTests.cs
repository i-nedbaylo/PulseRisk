using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PulseRisk.Domain.Enums;
using PulseRisk.Infrastructure.Persistence;
using PulseRisk.IntegrationTests.TestInfrastructure;

namespace PulseRisk.IntegrationTests.TradeProcessing;

public sealed class TradeProcessingPostgreSqlTests
{
    [SkippableFact]
    public async Task CreateTrade_ShouldPersistTradeAndUpdatePosition()
    {
        PostgreSqlTestContainer.SkipIfUnavailable();

        await using var postgres = PostgreSqlTestContainer.Create();
        await PostgreSqlTestContainer.StartOrSkipAsync(postgres);

        await using var application = new PulseRiskPostgresApplicationFactory(postgres.GetConnectionString());
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PulseRiskDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        using var client = application.CreateClient();
        var data = new IntegrationTestDataBuilder(client);

        var createdClient = await data.CreateClientAsync();

        var createdAccount = await data.CreateTradingAccountAsync(
            createdClient.Id,
            balance: 10_000m,
            CurrencyCode.USD,
            leverage: 100m);

        var createdTrade = await data.CreateTradeAsync(
            createdClient.Id,
            createdAccount.Id,
            symbol: "EURUSD",
            TradeSide.Buy,
            volume: 2m,
            openPrice: 1.25m);

        await using var assertionScope = application.Services.CreateAsyncScope();
        var assertionContext = assertionScope.ServiceProvider.GetRequiredService<PulseRiskDbContext>();

        var trade = await assertionContext.Trades
            .AsNoTracking()
            .SingleAsync(storedTrade => storedTrade.Id == createdTrade.Id);

        var position = await assertionContext.Positions
            .AsNoTracking()
            .SingleAsync(storedPosition => storedPosition.TradingAccountId == createdAccount.Id);

        trade.ClientId.Should().Be(createdClient.Id);
        trade.TradingAccountId.Should().Be(createdAccount.Id);
        trade.Symbol.Value.Should().Be("EURUSD");
        trade.Volume.Value.Should().Be(2m);
        trade.OpenPrice.Value.Should().Be(1.25m);

        position.ClientId.Should().Be(createdClient.Id);
        position.TradingAccountId.Should().Be(createdAccount.Id);
        position.Symbol.Value.Should().Be("EURUSD");
        position.NetVolume.Should().Be(2m);
        position.AveragePrice.Should().Be(1.25m);
        position.FloatingPnL.Should().Be(0m);
    }
}
