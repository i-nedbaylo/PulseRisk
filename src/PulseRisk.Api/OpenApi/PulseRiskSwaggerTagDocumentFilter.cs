using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PulseRisk.Api.OpenApi;

internal sealed class PulseRiskSwaggerTagDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = new HashSet<OpenApiTag>
        {
            new()
            {
                Name = "Clients",
                Description = "Client lifecycle and lookup operations."
            },
            new()
            {
                Name = "Accounts",
                Description = "Trading account creation and account lookup operations."
            },
            new()
            {
                Name = "Instruments",
                Description = "Tradable instrument reference data."
            },
            new()
            {
                Name = "Trades",
                Description = "Trade acceptance and position-changing operations."
            },
            new()
            {
                Name = "Health",
                Description = "API liveness endpoint."
            }
        };
    }
}
