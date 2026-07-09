using PulseRisk.Domain.Entities;

namespace PulseRisk.Application.Quotes;

internal static class LatestQuoteMappings
{
    public static LatestQuoteDto ToDto(this Quote quote)
    {
        return new LatestQuoteDto(
            quote.Id,
            quote.Symbol.Value,
            quote.Bid.Value,
            quote.Ask.Value,
            quote.Timestamp);
    }
}
