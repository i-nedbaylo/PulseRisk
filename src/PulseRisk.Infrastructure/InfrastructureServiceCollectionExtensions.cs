using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PulseRisk.Application.Common;
using PulseRisk.Application.Events;
using PulseRisk.Application.Repositories;
using PulseRisk.Infrastructure.Events;
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

        services.Configure<EventChannelOptions>(
            configuration.GetSection(EventChannelOptions.SectionName));

        services.AddEventChannel<QuoteTick>();
        services.AddEventChannel<TradeAcceptedEvent>();
        services.AddEventChannel<PositionChangedEvent>();
        services.AddEventChannel<RiskEvaluationRequested>();
        services.AddEventChannel<RiskAlertRaisedEvent>();

        return services;
    }

    private static IServiceCollection AddEventChannel<TEvent>(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryEventChannel<TEvent>>();
        services.AddSingleton<IEventWriter<TEvent>>(provider =>
            provider.GetRequiredService<InMemoryEventChannel<TEvent>>());
        services.AddSingleton<IEventReader<TEvent>>(provider =>
            provider.GetRequiredService<InMemoryEventChannel<TEvent>>());

        return services;
    }
}
