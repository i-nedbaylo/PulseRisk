namespace PulseRisk.Application.Events;

public sealed class EventChannelOptions
{
    public const string SectionName = "EventChannels";

    public int DefaultCapacity { get; init; } = 1024;

    public EventChannelFullMode DefaultFullMode { get; init; } = EventChannelFullMode.Wait;

    public int QuoteCapacity { get; init; } = 4096;

    public EventChannelFullMode QuoteFullMode { get; init; } = EventChannelFullMode.DropOldest;

    public int RiskCapacity { get; init; } = 1024;

    public EventChannelFullMode RiskFullMode { get; init; } = EventChannelFullMode.Wait;

    public EventChannelSettings GetSettingsFor<TEvent>()
    {
        if (typeof(TEvent) == typeof(QuoteTick))
        {
            return new EventChannelSettings(NormalizeCapacity(QuoteCapacity), QuoteFullMode);
        }

        if (typeof(TEvent) == typeof(PositionChangedEvent)
            || typeof(TEvent) == typeof(RiskEvaluationRequested)
            || typeof(TEvent) == typeof(RiskAlertRaisedEvent))
        {
            return new EventChannelSettings(NormalizeCapacity(RiskCapacity), RiskFullMode);
        }

        return new EventChannelSettings(NormalizeCapacity(DefaultCapacity), DefaultFullMode);
    }

    private static int NormalizeCapacity(int capacity)
    {
        return capacity > 0 ? capacity : 1;
    }
}
