using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PulseRisk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "instruments",
                columns: table => new
                {
                    symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    base_asset = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    quote_asset = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    digits = table.Column<int>(type: "integer", nullable: false),
                    contract_size = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instruments", x => x.symbol);
                });

            migrationBuilder.CreateTable(
                name: "risk_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    rule_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    threshold_value = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_risk_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "trading_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    leverage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trading_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_trading_accounts_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "quotes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    bid = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    ask = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_quotes", x => x.id);
                    table.ForeignKey(
                        name: "fk_quotes_instruments_symbol",
                        column: x => x.symbol,
                        principalTable: "instruments",
                        principalColumn: "symbol",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    net_volume = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    average_price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    floating_pn_l = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_positions", x => x.id);
                    table.ForeignKey(
                        name: "fk_positions_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_positions_instruments_symbol",
                        column: x => x.symbol,
                        principalTable: "instruments",
                        principalColumn: "symbol",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_positions_trading_accounts_trading_account_id",
                        column: x => x.trading_account_id,
                        principalTable: "trading_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "risk_alerts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    alert_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_risk_alerts", x => x.id);
                    table.ForeignKey(
                        name: "fk_risk_alerts_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_risk_alerts_instruments_symbol",
                        column: x => x.symbol,
                        principalTable: "instruments",
                        principalColumn: "symbol",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_risk_alerts_trading_accounts_trading_account_id",
                        column: x => x.trading_account_id,
                        principalTable: "trading_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "trades",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trading_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    symbol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    side = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    volume = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    open_price = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_trades", x => x.id);
                    table.ForeignKey(
                        name: "fk_trades_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trades_instruments_symbol",
                        column: x => x.symbol,
                        principalTable: "instruments",
                        principalColumn: "symbol",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_trades_trading_accounts_trading_account_id",
                        column: x => x.trading_account_id,
                        principalTable: "trading_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "instruments",
                columns: new[] { "symbol", "base_asset", "contract_size", "digits", "is_active", "quote_asset" },
                values: new object[,]
                {
                    { "EURUSD", "EUR", 100000m, 5, true, "USD" },
                    { "GBPUSD", "GBP", 100000m, 5, true, "USD" },
                    { "XAUUSD", "XAU", 100m, 2, true, "USD" }
                });

            migrationBuilder.InsertData(
                table: "risk_rules",
                columns: new[] { "id", "is_enabled", "name", "rule_type", "severity", "threshold_value" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), true, "Max exposure limit", "MaxExposureLimit", "Critical", 1000000m },
                    { new Guid("10000000-0000-0000-0000-000000000002"), true, "Max loss limit", "MaxLossLimit", "Critical", -25000m },
                    { new Guid("10000000-0000-0000-0000-000000000003"), true, "Margin level warning", "MarginLevelWarning", "Warning", 100m },
                    { new Guid("10000000-0000-0000-0000-000000000004"), true, "Price spike detection", "PriceSpikeDetection", "Warning", 2.5m },
                    { new Guid("10000000-0000-0000-0000-000000000005"), true, "High frequency trading activity", "HighFrequencyTradingActivity", "Warning", 100m }
                });

            migrationBuilder.CreateIndex(
                name: "ix_positions_client_id_symbol",
                table: "positions",
                columns: new[] { "client_id", "symbol" });

            migrationBuilder.CreateIndex(
                name: "ix_positions_symbol",
                table: "positions",
                column: "symbol");

            migrationBuilder.CreateIndex(
                name: "ix_positions_trading_account_id_symbol",
                table: "positions",
                columns: new[] { "trading_account_id", "symbol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_quotes_symbol_timestamp",
                table: "quotes",
                columns: new[] { "symbol", "timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_risk_alerts_client_id_created_at",
                table: "risk_alerts",
                columns: new[] { "client_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_risk_alerts_client_id_trading_account_id_symbol_alert_type",
                table: "risk_alerts",
                columns: new[] { "client_id", "trading_account_id", "symbol", "alert_type" },
                filter: "resolved_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_risk_alerts_resolved_at",
                table: "risk_alerts",
                column: "resolved_at");

            migrationBuilder.CreateIndex(
                name: "ix_risk_alerts_severity_created_at",
                table: "risk_alerts",
                columns: new[] { "severity", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_risk_alerts_symbol",
                table: "risk_alerts",
                column: "symbol");

            migrationBuilder.CreateIndex(
                name: "ix_risk_alerts_trading_account_id",
                table: "risk_alerts",
                column: "trading_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_risk_rules_rule_type_is_enabled",
                table: "risk_rules",
                columns: new[] { "rule_type", "is_enabled" });

            migrationBuilder.CreateIndex(
                name: "ix_trades_client_id_created_at",
                table: "trades",
                columns: new[] { "client_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_trades_symbol_created_at",
                table: "trades",
                columns: new[] { "symbol", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_trades_trading_account_id_created_at",
                table: "trades",
                columns: new[] { "trading_account_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_trading_accounts_client_id",
                table: "trading_accounts",
                column: "client_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "positions");

            migrationBuilder.DropTable(
                name: "quotes");

            migrationBuilder.DropTable(
                name: "risk_alerts");

            migrationBuilder.DropTable(
                name: "risk_rules");

            migrationBuilder.DropTable(
                name: "trades");

            migrationBuilder.DropTable(
                name: "instruments");

            migrationBuilder.DropTable(
                name: "trading_accounts");

            migrationBuilder.DropTable(
                name: "clients");
        }
    }
}
