using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PulseRisk.BackgroundWorkers;

public static class BackgroundWorkersServiceCollectionExtensions
{
    public static IServiceCollection AddPulseRiskBackgroundWorkers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;

        return services;
    }
}

