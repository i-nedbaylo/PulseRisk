namespace PulseRisk.Application.Events;

public interface IEventChannelLifetime
{
    string EventName { get; }

    void Complete(Exception? error = null);
}
