using System.Threading.Channels;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PulseRisk.Application.Events;

namespace PulseRisk.Infrastructure.Events;

internal sealed class InMemoryEventChannel<TEvent>(
    IOptions<EventChannelOptions> options,
    ILogger<InMemoryEventChannel<TEvent>> logger) :
    IEventWriter<TEvent>,
    IEventReader<TEvent>,
    IEventChannelMonitor,
    IEventChannelLifetime
{
    private readonly EventChannelSettings _settings = options.Value.GetSettingsFor<TEvent>();
    private readonly Channel<TEvent> _channel = CreateChannel(options.Value.GetSettingsFor<TEvent>());
    private int _currentDepth;
    private int _isCompleted;
    private long _writtenMessages;
    private long _readMessages;
    private long _droppedMessages;

    public string EventName { get; } = typeof(TEvent).Name;

    public async ValueTask WriteAsync(TEvent message, CancellationToken cancellationToken)
    {
        var wasFull = Volatile.Read(ref _currentDepth) >= _settings.Capacity;

        if (wasFull)
        {
            RecordDroppedMessage();

            if (_settings.FullMode == EventChannelFullMode.DropWrite)
            {
                return;
            }
        }

        await _channel.Writer.WriteAsync(message, cancellationToken);

        Interlocked.Increment(ref _writtenMessages);

        if (!wasFull)
        {
            Interlocked.Increment(ref _currentDepth);
        }
    }

    public IAsyncEnumerable<TEvent> ReadAllAsync(CancellationToken cancellationToken)
    {
        return ReadAllCoreAsync(cancellationToken);
    }

    public EventChannelSnapshot GetSnapshot()
    {
        return new EventChannelSnapshot(
            EventName,
            _settings.Capacity,
            _settings.FullMode,
            Volatile.Read(ref _currentDepth),
            Volatile.Read(ref _writtenMessages),
            Volatile.Read(ref _readMessages),
            Volatile.Read(ref _droppedMessages),
            Volatile.Read(ref _isCompleted) == 1);
    }

    public void Complete(Exception? error = null)
    {
        if (_channel.Writer.TryComplete(error))
        {
            Volatile.Write(ref _isCompleted, 1);
            logger.LogInformation("Event channel {EventName} completed.", EventName);
        }
    }

    private async IAsyncEnumerable<TEvent> ReadAllCoreAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            Interlocked.Decrement(ref _currentDepth);
            Interlocked.Increment(ref _readMessages);

            yield return message;
        }
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

    private void RecordDroppedMessage()
    {
        var droppedMessages = Interlocked.Increment(ref _droppedMessages);

        logger.LogWarning(
            "Event channel {EventName} is full. FullMode {FullMode}. Dropped messages: {DroppedMessages}.",
            EventName,
            _settings.FullMode,
            droppedMessages);
    }
}
