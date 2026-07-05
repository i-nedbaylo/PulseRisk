using Microsoft.EntityFrameworkCore;
using PulseRisk.Infrastructure.Persistence;

namespace PulseRisk.Api.Startup;

public static class DatabaseMigrationExtensions
{
    private const string ApplyMigrationsKey = "Database:ApplyMigrationsOnStartup";

    public static async Task<WebApplication> ApplyPulseRiskDatabaseMigrationsAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>(ApplyMigrationsKey))
        {
            return app;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PulseRiskDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("PulseRisk.DatabaseMigration");

        logger.LogInformation("Applying PostgreSQL migrations on startup.");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("PostgreSQL migrations were applied.");

        return app;
    }
}
