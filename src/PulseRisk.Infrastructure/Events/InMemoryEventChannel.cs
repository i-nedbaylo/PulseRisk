using System.Threading.Channels;
using Microsoft.Extensions.Options;
using PulseRisk.Application.Events;

namespace PulseRisk.Infrastructure.Events;

internal sealed class InMemoryEventChannel<TEvent>(
    IOptions<EventChannelOptions> options) : IEventWriter<TEvent>, IEventReader<TEvent>
{
    private readonly Channel<TEvent> _channel = CreateChannel(options.Value.GetSettingsFor<TEvent>());

    public ValueTask WriteAsync(TEvent message, CancellationToken cancellationToken)
    {
        return _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public IAsyncEnumerable<TEvent> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    private static Channel<TEvent> CreateChannel(EventChannelSettings settings)
    {
        return Channel.CreateBounded<TEvent>(
            new BoundedChannelOptions(settings.Capacity)
            {
                FullMode = MapFullMode(settings.FullMode),
                SingleReader = false,
                SingleWriter = false
            });
    }

    private static BoundedChannelFullMode MapFullMode(EventChannelFullMode fullMode)
    {
        return fullMode switch
        {
            EventChannelFullMode.Wait => BoundedChannelFullMode.Wait,
            EventChannelFullMode.DropNewest => BoundedChannelFullMode.DropNewest,
            EventChannelFullMode.DropOldest => BoundedChannelFullMode.DropOldest,
            EventChannelFullMode.DropWrite => BoundedChannelFullMode.DropWrite,
            _ => BoundedChannelFullMode.Wait
        };
    }
}
