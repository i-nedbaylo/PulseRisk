using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;

namespace PulseRisk.Api.OpenApi;

public static class SwaggerServiceCollectionExtensions
{
    public static IServiceCollection AddPulseRiskSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "PulseRisk API",
                Version = "v1",
                Description = "Educational real-time risk-management backend API."
            });

            options.TagActionsBy(apiDescription => [ResolveTag(apiDescription)]);
            options.OperationFilter<PulseRiskSwaggerOperationFilter>();
            options.DocumentFilter<PulseRiskSwaggerTagDocumentFilter>();
        });

        return services;
    }

    public static WebApplication UsePulseRiskSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.DocumentTitle = "PulseRisk API";
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "PulseRisk API v1");
            options.DisplayRequestDuration();
        });

        return app;
    }

    private static string ResolveTag(ApiDescription apiDescription)
    {
        var controller = apiDescription.ActionDescriptor.RouteValues["controller"];

        return controller switch
        {
            "Clients" => "Clients",
            "Accounts" => "Accounts",
            "Instruments" => "Instruments",
            "Trades" => "Trades",
            "Health" => "Health",
            _ => "Other"
        };
    }
}
