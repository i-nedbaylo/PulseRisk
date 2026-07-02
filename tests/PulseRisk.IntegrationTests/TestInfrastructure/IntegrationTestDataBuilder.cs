using System.Net.Http.Json;
using FluentAssertions;
using PulseRisk.Application.Accounts;
using PulseRisk.Application.Clients;
using PulseRisk.Application.Trades;
using PulseRisk.Domain.Enums;

namespace PulseRisk.IntegrationTests.TestInfrastructure;

internal sealed class IntegrationTestDataBuilder(HttpClient client)
{
    public Task<ClientDto> CreateClientAsync(string name = "Acme Capital")
    {
        return PostAsync<ClientDto>(
            "/api/clients",
            new CreateClientCommand(name));
    }

    public Task<TradingAccountDto> CreateTradingAccountAsync(
        Guid clientId,
        decimal balance = 10_000m,
        CurrencyCode currency = CurrencyCode.USD,
        decimal leverage = 100m)
    {
        return PostAsync<TradingAccountDto>(
            "/api/accounts",
            new CreateTradingAccountCommand(
                clientId,
                balance,
                currency,
                leverage));
    }

    public Task<TradeDto> CreateTradeAsync(
        Guid clientId,
        Guid tradingAccountId,
        string symbol = "EURUSD",
        TradeSide side = TradeSide.Buy,
        decimal volume = 2m,
        decimal openPrice = 1.25m)
    {
        return PostAsync<TradeDto>(
            "/api/trades",
            new CreateTradeCommand(
                clientId,
                tradingAccountId,
                symbol,
                side,
                volume,
                openPrice));
    }

    private async Task<TResponse> PostAsync<TResponse>(
        string requestUri,
        object body)
    {
        var response = await client.PostAsJsonAsync(requestUri, body);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadFromJsonAsync<TResponse>();
        responseBody.Should().NotBeNull();

        return responseBody!;
    }
}
