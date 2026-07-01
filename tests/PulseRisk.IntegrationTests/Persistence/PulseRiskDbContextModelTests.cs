using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using PulseRisk.Domain.Entities;
using PulseRisk.Infrastructure.Persistence;

namespace PulseRisk.IntegrationTests.Persistence;

public sealed class PulseRiskDbContextModelTests
{
    [Fact]
    public void Model_ShouldUseExpectedTableNames()
    {
        using var context = new PulseRiskDesignTimeDbContextFactory().CreateDbContext([]);

        context.Model.FindEntityType(typeof(Client))!.GetTableName().Should().Be("clients");
        context.Model.FindEntityType(typeof(TradingAccount))!.GetTableName().Should().Be("trading_accounts");
        context.Model.FindEntityType(typeof(Instrument))!.GetTableName().Should().Be("instruments");
        context.Model.FindEntityType(typeof(Trade))!.GetTableName().Should().Be("trades");
        context.Model.FindEntityType(typeof(Position))!.GetTableName().Should().Be("positions");
        context.Model.FindEntityType(typeof(Quote))!.GetTableName().Should().Be("quotes");
        context.Model.FindEntityType(typeof(RiskRule))!.GetTableName().Should().Be("risk_rules");
        context.Model.FindEntityType(typeof(RiskAlert))!.GetTableName().Should().Be("risk_alerts");
    }

    [Fact]
    public void TradeModel_ShouldContainQueryIndexes()
    {
        using var context = new PulseRiskDesignTimeDbContextFactory().CreateDbContext([]);

        var tradeEntity = context.Model.FindEntityType(typeof(Trade))!;
        var indexNames = tradeEntity.GetIndexes()
            .Select(index => index.GetDatabaseName())
            .ToArray();

        indexNames.Should().Contain([
            "ix_trades_client_id_created_at",
            "ix_trades_symbol_created_at",
            "ix_trades_trading_account_id_created_at"
        ]);
    }

    [Fact]
    public void RiskAlertModel_ShouldContainActiveAlertPartialIndex()
    {
        using var context = new PulseRiskDesignTimeDbContextFactory().CreateDbContext([]);

        var alertEntity = context.Model.FindEntityType(typeof(RiskAlert))!;
        var activeAlertIndex = alertEntity.GetIndexes()
            .Single(index => index.GetDatabaseName() == "ix_risk_alerts_client_id_trading_account_id_symbol_alert_type");

        activeAlertIndex.GetFilter().Should().Be("resolved_at IS NULL");
    }

    [Fact]
    public void PositionModel_ShouldUseXminConcurrencyToken()
    {
        using var context = new PulseRiskDesignTimeDbContextFactory().CreateDbContext([]);

        var positionEntity = context.Model.FindEntityType(typeof(Position))!;
        var xmin = positionEntity.FindProperty("xmin");

        xmin.Should().NotBeNull();
        xmin!.IsConcurrencyToken.Should().BeTrue();
        xmin.ValueGenerated.Should().Be(ValueGenerated.OnAddOrUpdate);
        xmin.GetColumnType().Should().Be("xid");
    }
}
