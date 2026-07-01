using Microsoft.Extensions.DependencyInjection;
using PulseRisk.Application.Accounts;
using PulseRisk.Application.Clients;
using PulseRisk.Application.Instruments;
using PulseRisk.Application.Trades;

namespace PulseRisk.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddPulseRiskApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<CreateClientValidator>();
        services.AddScoped<FluentValidation.IValidator<CreateClientCommand>>(provider =>
            provider.GetRequiredService<CreateClientValidator>());
        services.AddScoped<CreateClientHandler>();
        services.AddScoped<GetClientByIdHandler>();
        services.AddScoped<GetClientsHandler>();

        services.AddScoped<CreateTradingAccountValidator>();
        services.AddScoped<FluentValidation.IValidator<CreateTradingAccountCommand>>(provider =>
            provider.GetRequiredService<CreateTradingAccountValidator>());
        services.AddScoped<CreateTradingAccountHandler>();
        services.AddScoped<GetTradingAccountByIdHandler>();
        services.AddScoped<GetClientAccountsHandler>();

        services.AddScoped<CreateInstrumentValidator>();
        services.AddScoped<FluentValidation.IValidator<CreateInstrumentCommand>>(provider =>
            provider.GetRequiredService<CreateInstrumentValidator>());
        services.AddScoped<CreateInstrumentHandler>();
        services.AddScoped<GetInstrumentsHandler>();

        services.AddScoped<CreateTradeValidator>();
        services.AddScoped<FluentValidation.IValidator<CreateTradeCommand>>(provider =>
            provider.GetRequiredService<CreateTradeValidator>());
        services.AddScoped<CreateTradeHandler>();
        services.AddScoped<GetTradeByIdHandler>();

        return services;
    }
}
