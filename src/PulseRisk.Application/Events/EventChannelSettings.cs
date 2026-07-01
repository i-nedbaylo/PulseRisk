namespace PulseRisk.Application.Events;

public sealed record EventChannelSettings(int Capacity, EventChannelFullMode FullMode);
