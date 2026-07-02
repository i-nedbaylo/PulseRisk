using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PulseRisk.Application.Events;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.Enums;
using PulseRisk.Infrastructure.Persistence;
using PulseRisk.IntegrationTests.TestInfrastructure;

namespace PulseRisk.IntegrationTests.RiskEngine;

public sealed class RiskAlertPostgreSqlTests
{
    [SkippableFact]
    public async Task CreateTrade_WhenExposureLimitIsExceeded_ShouldPersistRiskAlert()
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

        var quoteTimestamp = new DateTimeOffset(2026, 7, 2, 12, 0, 0, TimeSpan.Zero);
        await using (var quoteScope = application.Services.CreateAsyncScope())
        {
            var quoteWriter = quoteScope.ServiceProvider.GetRequiredService<IQuoteBatchWriter>();
            await quoteWriter.WriteAsync(
                [new QuoteTick("EURUSD", Bid: 1.30m, Ask: 1.31m, quoteTimestamp)],
                CancellationToken.None);
        }

        using var client = application.CreateClient();
        var data = new IntegrationTestDataBuilder(client);

        var createdClient = await data.CreateClientAsync("Risk Desk");

        var createdAccount = await data.CreateTradingAccountAsync(
            createdClient.Id,
            balance: 100_000m,
            CurrencyCode.USD,
            leverage: 100m);

        await data.CreateTradeAsync(
            createdClient.Id,
            createdAccount.Id,
            symbol: "EURUSD",
            TradeSide.Buy,
            volume: 10m,
            openPrice: 1.25m);

        var alert = await WaitForRiskAlertAsync(
            application,
            createdAccount.Id,
            RiskAlertType.ExposureLimitExceeded);

        alert.Should().NotBeNull();
        alert!.ClientId.Should().Be(createdClient.Id);
        alert.TradingAccountId.Should().Be(createdAccount.Id);
        alert.Symbol.Value.Should().Be("EURUSD");
        alert.AlertType.Should().Be(RiskAlertType.ExposureLimitExceeded);
        alert.Severity.Should().Be(RiskSeverity.Critical);
        alert.Message.Should().Contain("Net exposure");
        alert.ResolvedAt.Should().BeNull();
        alert.IsActive.Should().BeTrue();
    }

    private static async Task<RiskAlert?> WaitForRiskAlertAsync(
        PulseRiskPostgresApplicationFactory application,
        Guid tradingAccountId,
        RiskAlertType alertType)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);

        while (DateTimeOffset.UtcNow < deadline)
        {
            await using var scope = application.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PulseRiskDbContext>();
            var alert = await dbContext.RiskAlerts
                .AsNoTracking()
                .Where(candidate =>
                    candidate.TradingAccountId == tradingAccountId
                    && candidate.AlertType == alertType)
                .SingleOrDefaultAsync();

            if (alert is not null)
            {
                return alert;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        return null;
    }
}
