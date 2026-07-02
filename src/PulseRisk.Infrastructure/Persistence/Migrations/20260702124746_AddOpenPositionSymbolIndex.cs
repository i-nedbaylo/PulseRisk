using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseRisk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenPositionSymbolIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_positions_symbol_open_trading_account",
                table: "positions",
                columns: new[] { "symbol", "trading_account_id" },
                filter: "net_volume <> 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_positions_symbol_open_trading_account",
                table: "positions");
        }
    }
}
