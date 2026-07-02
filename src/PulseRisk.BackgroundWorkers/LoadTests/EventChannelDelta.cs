using PulseRisk.Application.Events;

namespace PulseRisk.BackgroundWorkers.LoadTests;

public sealed record EventChannelDelta(
    string EventName,
    int Capacity,
    EventChannelFullMode FullMode,
    int StartDepth,
    int EndDepth,
    long WrittenMessages,
    long ReadMessages,
    long DroppedMessages);
