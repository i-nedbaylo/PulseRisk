using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PulseRisk.Infrastructure.Persistence;

namespace PulseRisk.IntegrationTests.TestInfrastructure;

internal sealed class PulseRiskPostgresApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PulseRisk"] = connectionString
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PulseRiskDbContext>>();
            services.RemoveAll<PulseRiskDbContext>();
            services.AddDbContext<PulseRiskDbContext>(options =>
            {
                options
                    .UseNpgsql(connectionString)
                    .UseSnakeCaseNamingConvention();
            });
        });
    }
}
