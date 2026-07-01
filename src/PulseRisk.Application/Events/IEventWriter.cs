namespace PulseRisk.Application.Events;

public interface IEventWriter<in TEvent>
{
    ValueTask WriteAsync(TEvent message, CancellationToken cancellationToken);
}
