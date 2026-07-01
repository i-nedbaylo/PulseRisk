namespace PulseRisk.Application.Events;

public interface IEventChannelMonitor
{
    string EventName { get; }

    EventChannelSnapshot GetSnapshot();
}
