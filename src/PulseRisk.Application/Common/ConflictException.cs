namespace PulseRisk.Application.Common;

public sealed class ConflictException(string message) : Exception(message);

