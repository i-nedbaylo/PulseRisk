using PulseRisk.Application.Events;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.BackgroundWorkers.MarketData;

public sealed class MarketDataQuoteGenerator
{
    private readonly Random _random;
    private readonly Dictionary<string, decimal> _lastMidPrices = new(StringComparer.OrdinalIgnoreCase);

    public MarketDataQuoteGenerator()
        : this(Random.Shared)
    {
    }

    public MarketDataQuoteGenerator(Random random)
    {
        _random = random;
    }

    public QuoteTick NextTick(
        string symbol,
        MarketDataOptions options,
        DateTimeOffset timestamp)
    {
        var normalizedSymbol = new Symbol(symbol).Value;
        var previousMidPrice = GetLastMidPrice(normalizedSymbol);
        var priceStep = previousMidPrice * options.PriceStepPercent * NextSignedFactor();
        var midPrice = Math.Max(options.MinimumPrice, previousMidPrice + priceStep);
        var halfSpread = Math.Max(options.MinimumPrice / 2, midPrice * options.SpreadPercent / 2);

        var bid = RoundPrice(Math.Max(options.MinimumPrice, midPrice - halfSpread));
        var ask = RoundPrice(Math.Max(bid + options.MinimumPrice, midPrice + halfSpread));

        _lastMidPrices[normalizedSymbol] = midPrice;

        return new QuoteTick(
            normalizedSymbol,
            bid,
            ask,
            timestamp);
    }

    private decimal GetLastMidPrice(string symbol)
    {
        if (_lastMidPrices.TryGetValue(symbol, out var price))
        {
            return price;
        }

        return symbol switch
        {
            "EURUSD" => 1.10000m,
            "GBPUSD" => 1.27000m,
            "XAUUSD" => 2350.00m,
            _ => 100.00m
        };
    }

    private decimal NextSignedFactor()
    {
        return (decimal)(_random.NextDouble() * 2d - 1d);
    }

    private static decimal RoundPrice(decimal value)
    {
        return decimal.Round(value, decimals: 8, MidpointRounding.AwayFromZero);
    }
}
