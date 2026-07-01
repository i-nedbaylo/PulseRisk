using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseRisk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionXminConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // xmin is a PostgreSQL system column; this migration only updates the EF model snapshot.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The physical xmin column is managed by PostgreSQL and must not be dropped.
        }
    }
}
