using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PulseRisk.Application.Common;
using PulseRisk.Application.Events;
using PulseRisk.Application.Repositories;
using PulseRisk.Application.Risk;
using PulseRisk.Application.Trades;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;
using PulseRisk.Domain.ValueObjects;
using PulseRisk.Infrastructure.Persistence;
using PulseRisk.IntegrationTests.TestInfrastructure;

namespace PulseRisk.IntegrationTests.ReadApi;

public sealed class ReadApiPostgreSqlTests
{
    [SkippableFact]
    public async Task GetTrades_WhenClientIdFilterIsProvided_ShouldReturnOnlyClientTrades()
    {
        PostgreSqlTestContainer.SkipIfUnavailable();

        await using var postgres = PostgreSqlTestContainer.Create();
        await PostgreSqlTestContainer.StartOrSkipAsync(postgres);

        await using var application = new PulseRiskPostgresApplicationFactory(postgres.GetConnectionString());
        await ApplyMigrationsAsync(application);

        using var client = application.CreateClient();
        var data = new IntegrationTestDataBuilder(client);

        var firstClient = await data.CreateClientAsync("First Desk");
        var secondClient = await data.CreateClientAsync("Second Desk");
        var firstAccount = await data.CreateTradingAccountAsync(firstClient.Id);
        var secondAccount = await data.CreateTradingAccountAsync(secondClient.Id);

        await data.CreateTradeAsync(
            firstClient.Id,
            firstAccount.Id,
            symbol: "EURUSD",
            TradeSide.Buy,
            volume: 1m,
            openPrice: 1.20m);

        await data.CreateTradeAsync(
            firstClient.Id,
            firstAccount.Id,
            symbol: "GBPUSD",
            TradeSide.Sell,
            volume: 2m,
            openPrice: 1.40m);

        await data.CreateTradeAsync(
            secondClient.Id,
            secondAccount.Id,
            symbol: "EURUSD",
            TradeSide.Buy,
            volume: 3m,
            openPrice: 1.25m);

        var response = await client.GetAsync($"/api/trades?clientId={firstClient.Id}&pageSize=1");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TradeDto>>();

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(1);
        result.TotalPages.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
        result.Items.Should().ContainSingle();
        result.Items.Should().OnlyContain(trade => trade.ClientId == firstClient.Id);
    }

    [SkippableFact]
    public async Task GetRiskAlerts_WhenSeverityFilterIsProvided_ShouldReturnOnlyMatchingAlerts()
    {
        PostgreSqlTestContainer.SkipIfUnavailable();

        await using var postgres = PostgreSqlTestContainer.Create();
        await PostgreSqlTestContainer.StartOrSkipAsync(postgres);

        await using var application = new PulseRiskPostgresApplicationFactory(postgres.GetConnectionString());
        await ApplyMigrationsAsync(application);

        using var client = application.CreateClient();
        var data = new IntegrationTestDataBuilder(client);

        var createdClient = await data.CreateClientAsync("Risk Reader");
        var account = await data.CreateTradingAccountAsync(createdClient.Id);

        await using (var scope = application.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PulseRiskDbContext>();
            dbContext.RiskAlerts.AddRange(
                new RiskAlert(
                    Guid.NewGuid(),
                    createdClient.Id,
                    account.Id,
                    new Symbol("EURUSD"),
                    RiskAlertType.ExposureLimitExceeded,
                    RiskSeverity.Critical,
                    "Critical exposure alert.",
                    new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero),
                    resolvedAt: null),
                new RiskAlert(
                    Guid.NewGuid(),
                    createdClient.Id,
                    account.Id,
                    new Symbol("GBPUSD"),
                    RiskAlertType.MarginLevelWarning,
                    RiskSeverity.Warning,
                    "Warning margin alert.",
                    new DateTimeOffset(2026, 7, 10, 12, 1, 0, TimeSpan.Zero),
                    resolvedAt: null));

            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync("/api/risk/alerts?severity=Critical");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<RiskAlertDto>>();

        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();

        var alert = result.Items.Single();
        alert.ClientId.Should().Be(createdClient.Id);
        alert.TradingAccountId.Should().Be(account.Id);
        alert.Symbol.Should().Be("EURUSD");
        alert.Severity.Should().Be(RiskSeverity.Critical);
        alert.IsActive.Should().BeTrue();
    }

    [SkippableFact]
    public async Task GetClientRiskMetrics_WhenOpenPositionAndQuoteExist_ShouldReturnCurrentMetrics()
    {
        PostgreSqlTestContainer.SkipIfUnavailable();

        await using var postgres = PostgreSqlTestContainer.Create();
        await PostgreSqlTestContainer.StartOrSkipAsync(postgres);

        await using var application = new PulseRiskPostgresApplicationFactory(postgres.GetConnectionString());
        await ApplyMigrationsAsync(application);

        await using (var quoteScope = application.Services.CreateAsyncScope())
        {
            var quoteWriter = quoteScope.ServiceProvider.GetRequiredService<IQuoteBatchWriter>();
            await quoteWriter.WriteAsync(
                [new QuoteTick(
                    "EURUSD",
                    Bid: 1.30m,
                    Ask: 1.31m,
                    new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero))],
                CancellationToken.None);
        }

        using var client = application.CreateClient();
        var data = new IntegrationTestDataBuilder(client);

        var createdClient = await data.CreateClientAsync("Metrics Reader");
        var account = await data.CreateTradingAccountAsync(
            createdClient.Id,
            balance: 10_000m,
            CurrencyCode.USD,
            leverage: 100m);

        await data.CreateTradeAsync(
            createdClient.Id,
            account.Id,
            symbol: "EURUSD",
            TradeSide.Buy,
            volume: 1m,
            openPrice: 1.20m);

        var response = await client.GetAsync($"/api/risk/clients/{createdClient.Id}");
        response.EnsureSuccessStatusCode();

        var metrics = await response.Content.ReadFromJsonAsync<ClientRiskMetricDto[]>();

        metrics.Should().NotBeNull();
        metrics.Should().ContainSingle();

        var metric = metrics![0];
        metric.ClientId.Should().Be(createdClient.Id);
        metric.TradingAccountId.Should().Be(account.Id);
        metric.Symbol.Should().Be("EURUSD");
        metric.NetVolume.Should().Be(1m);
        metric.AveragePrice.Should().Be(1.20m);
        metric.NetExposure.Should().Be(120_000m);
        metric.FloatingPnL.Should().Be(10_000m);
        metric.Equity.Should().Be(20_000m);
        metric.MarginUsed.Should().Be(1_200m);
        metric.MarginLevel.Should().BeApproximately(1_666.6667m, 0.0001m);
        metric.LatestQuoteTimestamp.Should().Be(new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
    }

    private static async Task ApplyMigrationsAsync(PulseRiskPostgresApplicationFactory application)
    {
        await using var scope = application.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PulseRiskDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
