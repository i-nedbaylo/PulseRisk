namespace PulseRisk.Application.Common;

public sealed class ConcurrencyConflictException : ConflictException
{
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
