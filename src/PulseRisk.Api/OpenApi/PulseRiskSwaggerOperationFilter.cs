using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi;
using PulseRisk.Application.Accounts;
using PulseRisk.Application.Clients;
using PulseRisk.Application.Instruments;
using PulseRisk.Application.Trades;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PulseRisk.Api.OpenApi;

internal sealed class PulseRiskSwaggerOperationFilter : IOperationFilter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, OperationDocumentation> Documentation =
        new Dictionary<string, OperationDocumentation>
        {
            ["Clients.Create"] = new(
                "Create client",
                "Registers a client that can own trading accounts and positions."),
            ["Clients.Get"] = new(
                "List clients",
                "Returns clients ordered by creation time."),
            ["Clients.GetById"] = new(
                "Get client",
                "Returns one client by identifier."),
            ["Accounts.Create"] = new(
                "Create trading account",
                "Creates a trading account for an existing client."),
            ["Accounts.GetById"] = new(
                "Get trading account",
                "Returns one trading account by identifier."),
            ["Accounts.GetByClientId"] = new(
                "List client accounts",
                "Returns all trading accounts owned by a client."),
            ["Instruments.Create"] = new(
                "Create instrument",
                "Adds a tradable instrument with precision and contract-size metadata."),
            ["Instruments.Get"] = new(
                "List instruments",
                "Returns active and inactive instruments known by the system."),
            ["Trades.Create"] = new(
                "Create trade",
                "Accepts a trade, persists it and updates the aggregate position in one commit."),
            ["Trades.GetById"] = new(
                "Get trade",
                "Returns one trade by identifier."),
            ["Health.Get"] = new(
                "Health check",
                "Returns basic API liveness information.")
        };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor descriptor)
        {
            return;
        }

        var documentationKey = $"{descriptor.ControllerName}.{descriptor.ActionName}";
        if (Documentation.TryGetValue(documentationKey, out var documentation))
        {
            operation.Summary = documentation.Summary;
            operation.Description = documentation.Description;
        }

        operation.OperationId = $"{descriptor.ControllerName}_{descriptor.ActionName}";

        AddRequestExample(operation, context.ApiDescription);
        AddProblemResponses(operation, context, descriptor);
    }

    private static void AddRequestExample(OpenApiOperation operation, ApiDescription apiDescription)
    {
        var bodyType = apiDescription.ParameterDescriptions
            .FirstOrDefault(parameter => parameter.Source == BindingSource.Body)
            ?.Type;

        if (bodyType is null || CreateRequestExample(bodyType) is not { } example)
        {
            return;
        }

        if (operation.RequestBody?.Content is null)
        {
            return;
        }

        foreach (var mediaType in operation.RequestBody.Content)
        {
            if (mediaType.Key.Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                mediaType.Value.Example = example.DeepClone();
            }
        }
    }

    private static JsonNode? CreateRequestExample(Type requestType)
    {
        if (requestType == typeof(CreateClientCommand))
        {
            return JsonSerializer.SerializeToNode(new
            {
                name = "Acme Capital"
            }, JsonOptions);
        }

        if (requestType == typeof(CreateTradingAccountCommand))
        {
            return JsonSerializer.SerializeToNode(new
            {
                clientId = "11111111-1111-1111-1111-111111111111",
                balance = 10000,
                currency = 1,
                leverage = 100
            }, JsonOptions);
        }

        if (requestType == typeof(CreateInstrumentCommand))
        {
            return JsonSerializer.SerializeToNode(new
            {
                symbol = "EURUSD",
                baseAsset = "EUR",
                quoteAsset = "USD",
                digits = 5,
                contractSize = 100000,
                isActive = true
            }, JsonOptions);
        }

        if (requestType == typeof(CreateTradeCommand))
        {
            return JsonSerializer.SerializeToNode(new
            {
                clientId = "11111111-1111-1111-1111-111111111111",
                tradingAccountId = "22222222-2222-2222-2222-222222222222",
                symbol = "EURUSD",
                side = 1,
                volume = 2,
                openPrice = 1.25
            }, JsonOptions);
        }

        return null;
    }

    private static void AddProblemResponses(
        OpenApiOperation operation,
        OperationFilterContext context,
        ControllerActionDescriptor descriptor)
    {
        var httpMethod = context.ApiDescription.HttpMethod;
        if (string.Equals(httpMethod, HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
        {
            AddProblemResponse(operation, context, StatusCodes.Status400BadRequest, "Validation failed.");
        }

        if (ShouldDocumentNotFound(context.ApiDescription, descriptor))
        {
            AddProblemResponse(operation, context, StatusCodes.Status404NotFound, "Entity was not found.");
        }

        if (string.Equals(httpMethod, HttpMethods.Post, StringComparison.OrdinalIgnoreCase)
            && descriptor.ControllerName is "Instruments" or "Trades")
        {
            AddProblemResponse(operation, context, StatusCodes.Status409Conflict, "Request conflicts with current state.");
        }

        AddProblemResponse(operation, context, StatusCodes.Status500InternalServerError, "Unexpected server error.");
    }

    private static bool ShouldDocumentNotFound(
        ApiDescription apiDescription,
        ControllerActionDescriptor descriptor)
    {
        return descriptor.ActionName == "GetById"
            || descriptor.ControllerName is "Accounts" or "Trades"
                && string.Equals(apiDescription.HttpMethod, HttpMethods.Post, StringComparison.OrdinalIgnoreCase);
    }

    private static void AddProblemResponse(
        OpenApiOperation operation,
        OperationFilterContext context,
        int statusCode,
        string description)
    {
        var statusCodeText = statusCode.ToString();
        operation.Responses ??= new OpenApiResponses();
        if (operation.Responses.ContainsKey(statusCodeText))
        {
            return;
        }

        operation.Responses[statusCodeText] = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/problem+json"] = new()
                {
                    Schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository)
                }
            }
        };
    }

    private sealed record OperationDocumentation(string Summary, string Description);
}
