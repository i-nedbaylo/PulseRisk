using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PulseRisk.BackgroundWorkers.LoadTests;
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

        services.AddOptions<LoadTestOptions>()
            .Bind(configuration.GetSection(LoadTestOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<MarketDataOptions>, MarketDataOptionsValidator>();
        services.AddSingleton<IValidateOptions<QuoteBatchOptions>, QuoteBatchOptionsValidator>();
        services.AddSingleton<IValidateOptions<LoadTestOptions>, LoadTestOptionsValidator>();
        services.AddSingleton<MarketDataQuoteGenerator>();
        services.AddScoped<QuoteRiskEvaluationDispatcher>();
        services.AddScoped<LoadTestScenarioRunner>();
        services.AddSingleton<ILoadTestReportWriter, MarkdownLoadTestReportWriter>();
        services.AddHostedService<QuoteBatchWriterWorker>();
        services.AddHostedService<MarketDataSimulatorWorker>();
        services.AddHostedService<LoadTestWorker>();

        services.AddSingleton<PositionChangedEventProcessor>();
        services.AddScoped<RiskEvaluationProcessor>();
        services.AddHostedService<PositionChangedEventWorker>();
        services.AddHostedService<RiskEvaluationRequestedWorker>();

        return services;
    }
}
