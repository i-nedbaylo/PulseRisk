using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Api.Controllers;

internal static class QuerySymbols
{
    public static Symbol? ParseOptional(string? symbol)
    {
        return string.IsNullOrWhiteSpace(symbol)
            ? null
            : new Symbol(symbol);
    }
}
