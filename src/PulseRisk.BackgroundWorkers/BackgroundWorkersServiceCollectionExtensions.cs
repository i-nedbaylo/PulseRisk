using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PulseRisk.BackgroundWorkers.RiskEvents;

namespace PulseRisk.BackgroundWorkers;

public static class BackgroundWorkersServiceCollectionExtensions
{
    public static IServiceCollection AddPulseRiskBackgroundWorkers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;

        services.AddSingleton<PositionChangedEventProcessor>();
        services.AddHostedService<PositionChangedEventWorker>();

        return services;
    }
}
