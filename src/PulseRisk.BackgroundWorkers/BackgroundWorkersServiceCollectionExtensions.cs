using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PulseRisk.BackgroundWorkers.MarketData;
using PulseRisk.BackgroundWorkers.RiskEvents;

namespace PulseRisk.BackgroundWorkers;

public static class BackgroundWorkersServiceCollectionExtensions
{
    public static IServiceCollection AddPulseRiskBackgroundWorkers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<MarketDataOptions>()
            .Bind(configuration.GetSection(MarketDataOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<MarketDataOptions>, MarketDataOptionsValidator>();
        services.AddSingleton<MarketDataQuoteGenerator>();
        services.AddHostedService<MarketDataSimulatorWorker>();

        services.AddSingleton<PositionChangedEventProcessor>();
        services.AddHostedService<PositionChangedEventWorker>();

        return services;
    }
}
