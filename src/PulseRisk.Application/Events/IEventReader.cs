namespace PulseRisk.Application.Events;

public interface IEventReader<TEvent>
{
    IAsyncEnumerable<TEvent> ReadAllAsync(CancellationToken cancellationToken);
}
