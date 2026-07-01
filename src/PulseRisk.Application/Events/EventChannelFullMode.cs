namespace PulseRisk.Application.Events;

public enum EventChannelFullMode
{
    Wait = 1,
    DropNewest = 2,
    DropOldest = 3,
    DropWrite = 4
}
