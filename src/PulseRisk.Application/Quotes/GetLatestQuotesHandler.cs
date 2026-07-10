using PulseRisk.Application.Repositories;

namespace PulseRisk.Application.Quotes;

public sealed class GetLatestQuotesHandler(ILatestQuoteReader latestQuotes)
{
    public async Task<IReadOnlyCollection<LatestQuoteDto>> HandleAsync(
        GetLatestQuotesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await latestQuotes.ListLatestAsync(query.Symbol, cancellationToken);

        return result.Select(quote => quote.ToDto()).ToArray();
    }
}
