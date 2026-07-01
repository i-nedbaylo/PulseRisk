using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PulseRisk.Application.Accounts;
using PulseRisk.Application.Clients;
using PulseRisk.Application.Trades;
using PulseRisk.Domain.Enums;
using PulseRisk.Infrastructure.Persistence;
using PulseRisk.IntegrationTests.TestInfrastructure;
using Testcontainers.PostgreSql;

namespace PulseRisk.IntegrationTests.TradeProcessing;

public sealed class TradeProcessingPostgreSqlTests
{
    [SkippableFact]
    public async Task CreateTrade_ShouldPersistTradeAndUpdatePosition()
    {
        Skip.IfNot(
            DockerAvailability.IsDockerAvailable(),
            "Docker is not available; skipping PostgreSQL Testcontainers scenario.");

        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("pulserisk_tests")
            .WithUsername("pulserisk")
            .WithPassword("pulserisk")
            .Build();

        await postgres.StartAsync();

        await using var application = new PulseRiskPostgresApplicationFactory(postgres.GetConnectionString());
        await using (var scope = application.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PulseRiskDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        using var client = application.CreateClient();

        var createdClient = await PostAsync<ClientDto>(
            client,
            "/api/clients",
            new CreateClientCommand("Acme Capital"));

        var createdAccount = await PostAsync<TradingAccountDto>(
            client,
            "/api/accounts",
            new CreateTradingAccountCommand(
                createdClient.Id,
                Balance: 10_000m,
                CurrencyCode.USD,
                Leverage: 100m));

        var createdTrade = await PostAsync<TradeDto>(
            client,
            "/api/trades",
            new CreateTradeCommand(
                createdClient.Id,
                createdAccount.Id,
                Symbol: "EURUSD",
                TradeSide.Buy,
                Volume: 2m,
                OpenPrice: 1.25m));

        await using var assertionScope = application.Services.CreateAsyncScope();
        var assertionContext = assertionScope.ServiceProvider.GetRequiredService<PulseRiskDbContext>();

        var trade = await assertionContext.Trades
            .AsNoTracking()
            .SingleAsync(storedTrade => storedTrade.Id == createdTrade.Id);

        var position = await assertionContext.Positions
            .AsNoTracking()
            .SingleAsync(storedPosition =>
                storedPosition.TradingAccountId == createdAccount.Id
                && storedPosition.Symbol.Value == "EURUSD");

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

    private static async Task<TResponse> PostAsync<TResponse>(
        HttpClient client,
        string requestUri,
        object body)
    {
        var response = await client.PostAsJsonAsync(requestUri, body);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadFromJsonAsync<TResponse>();
        responseBody.Should().NotBeNull();

        return responseBody!;
    }
}
