using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PulseRisk.IntegrationTests;

public sealed class SwaggerEndpointTests
{
    [Fact]
    public async Task GetSwaggerJson_ShouldExposeTagsAndRequestExamples()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        root.GetProperty("info").GetProperty("title").GetString().Should().Be("PulseRisk API");

        var tags = root.GetProperty("tags")
            .EnumerateArray()
            .Select(tag => tag.GetProperty("name").GetString())
            .ToArray();

        tags.Should().Contain(["Clients", "Accounts", "Instruments", "Trades", "Positions", "Risk", "Quotes", "Health"]);

        var tradePost = root
            .GetProperty("paths")
            .GetProperty("/api/trades")
            .GetProperty("post");

        tradePost.GetProperty("summary").GetString().Should().Be("Create trade");
        tradePost.GetProperty("tags")[0].GetString().Should().Be("Trades");

        var tradeExample = tradePost
            .GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("example");

        tradeExample.GetProperty("symbol").GetString().Should().Be("EURUSD");
        tradeExample.GetProperty("volume").GetDecimal().Should().Be(2m);
        tradeExample.GetProperty("openPrice").GetDecimal().Should().Be(1.25m);

        tradePost
            .GetProperty("responses")
            .TryGetProperty("409", out _)
            .Should()
            .BeTrue();

        var paths = root.GetProperty("paths");

        AssertQueryParametersAreOptional(
            paths.GetProperty("/api/trades").GetProperty("get"),
            "clientId",
            "tradingAccountId",
            "symbol",
            "side",
            "createdFrom",
            "createdTo",
            "pageNumber",
            "pageSize");

        AssertQueryParametersAreOptional(
            paths.GetProperty("/api/clients/{clientId}/trades").GetProperty("get"),
            "tradingAccountId",
            "symbol",
            "side",
            "createdFrom",
            "createdTo",
            "pageNumber",
            "pageSize");

        AssertQueryParametersAreOptional(
            paths.GetProperty("/api/positions").GetProperty("get"),
            "clientId",
            "tradingAccountId",
            "symbol",
            "openOnly");

        AssertQueryParametersAreOptional(
            paths.GetProperty("/api/clients/{clientId}/positions").GetProperty("get"),
            "tradingAccountId",
            "symbol",
            "openOnly");

        AssertQueryParametersAreOptional(
            paths.GetProperty("/api/risk/alerts").GetProperty("get"),
            "clientId",
            "tradingAccountId",
            "symbol",
            "alertType",
            "severity",
            "activeOnly",
            "createdFrom",
            "createdTo",
            "pageNumber",
            "pageSize");

        AssertQueryParametersAreOptional(
            paths.GetProperty("/api/quotes/latest").GetProperty("get"),
            "symbol");
    }

    private static void AssertQueryParametersAreOptional(
        JsonElement operation,
        params string[] parameterNames)
    {
        var parameters = operation
            .GetProperty("parameters")
            .EnumerateArray()
            .ToArray();

        foreach (var parameterName in parameterNames)
        {
            var parameter = parameters.Single(candidate =>
                candidate.GetProperty("name").GetString() == parameterName
                && candidate.GetProperty("in").GetString() == "query");

            if (parameter.TryGetProperty("required", out var required))
            {
                required.GetBoolean().Should().BeFalse($"{parameterName} is an optional query parameter");
            }
        }
    }
}
