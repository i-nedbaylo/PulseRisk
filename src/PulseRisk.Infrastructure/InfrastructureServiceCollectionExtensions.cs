using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PulseRisk.Application.Common;
using PulseRisk.Application.Repositories;
using PulseRisk.Infrastructure.Persistence;
using PulseRisk.Infrastructure.Persistence.Repositories;

namespace PulseRisk.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPulseRiskInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PulseRisk")
            ?? throw new InvalidOperationException("Connection string 'PulseRisk' is not configured.");

        services.AddDbContext<PulseRiskDbContext>(options =>
        {
            options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IClientRepository, EfClientRepository>();
        services.AddScoped<ITradingAccountRepository, EfTradingAccountRepository>();
        services.AddScoped<IInstrumentRepository, EfInstrumentRepository>();
        services.AddScoped<ITradeRepository, EfTradeRepository>();
        services.AddScoped<IPositionRepository, EfPositionRepository>();

        return services;
    }
}
