using PulseRisk.Domain.Entities;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Application.Repositories;

public interface ILatestQuoteReader
{
    Task<Quote?> GetLatestAsync(Symbol symbol, CancellationToken cancellationToken);
}
