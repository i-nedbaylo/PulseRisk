using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PulseRisk.Infrastructure.Persistence;

public sealed class PulseRiskDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PulseRiskDbContext>
{
    public PulseRiskDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PulseRiskDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=pulserisk;Username=pulserisk;Password=pulserisk")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new PulseRiskDbContext(options);
    }
}

