using PulseRisk.Application.Events;

namespace PulseRisk.Application.Repositories;

public interface IQuoteBatchWriter
{
    Task WriteAsync(IReadOnlyCollection<QuoteTick> ticks, CancellationToken cancellationToken);
}
