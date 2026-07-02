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

        services.AddOptions<QuoteBatchOptions>()
            .Bind(configuration.GetSection(QuoteBatchOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<MarketDataOptions>, MarketDataOptionsValidator>();
        services.AddSingleton<IValidateOptions<QuoteBatchOptions>, QuoteBatchOptionsValidator>();
        services.AddSingleton<MarketDataQuoteGenerator>();
        services.AddScoped<QuoteRiskEvaluationDispatcher>();
        services.AddHostedService<QuoteBatchWriterWorker>();
        services.AddHostedService<MarketDataSimulatorWorker>();

        services.AddSingleton<PositionChangedEventProcessor>();
        services.AddScoped<RiskEvaluationProcessor>();
        services.AddHostedService<PositionChangedEventWorker>();
        services.AddHostedService<RiskEvaluationRequestedWorker>();

        return services;
    }
}
