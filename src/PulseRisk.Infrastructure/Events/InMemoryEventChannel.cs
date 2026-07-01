using System.Threading.Channels;
using PulseRisk.Application.Events;

namespace PulseRisk.Infrastructure.Events;

internal sealed class InMemoryEventChannel<TEvent> : IEventWriter<TEvent>, IEventReader<TEvent>
{
    private readonly Channel<TEvent> _channel = Channel.CreateBounded<TEvent>(
        new BoundedChannelOptions(capacity: 1024)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });

    public ValueTask WriteAsync(TEvent message, CancellationToken cancellationToken)
    {
        return _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public IAsyncEnumerable<TEvent> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
