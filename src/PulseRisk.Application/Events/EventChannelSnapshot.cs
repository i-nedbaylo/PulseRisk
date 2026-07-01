namespace PulseRisk.Application.Events;

public sealed record EventChannelSnapshot(
    string EventName,
    int Capacity,
    EventChannelFullMode FullMode,
    int CurrentDepth,
    long WrittenMessages,
    long ReadMessages,
    long DroppedMessages,
    bool IsCompleted);
