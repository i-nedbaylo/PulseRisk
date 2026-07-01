namespace PulseRisk.Application.Common;

public sealed class EntityNotFoundException(string entityName, string key)
    : Exception($"{entityName} '{key}' was not found.")
{
    public string EntityName { get; } = entityName;

    public string Key { get; } = key;
}

