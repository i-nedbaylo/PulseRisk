using Microsoft.Extensions.DependencyInjection;

namespace PulseRisk.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddPulseRiskApplication(this IServiceCollection services)
    {
        return services;
    }
}

