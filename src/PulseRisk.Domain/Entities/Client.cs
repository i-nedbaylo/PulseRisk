using PulseRisk.Domain.Common;
using PulseRisk.Domain.Enums;

namespace PulseRisk.Domain.Entities;

public sealed class Client
{
    public Client(Guid id, string name, ClientStatus status, DateTimeOffset createdAt)
    {
        DomainValidation.EnsureNotEmpty(id, nameof(id));

        Id = id;
        Name = DomainValidation.EnsureNotBlank(name, nameof(name));
        Status = status;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public string Name { get; }

    public ClientStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public static Client Create(string name, DateTimeOffset createdAt)
    {
        return new Client(Guid.NewGuid(), name, ClientStatus.Active, createdAt);
    }

    public void Suspend()
    {
        Status = ClientStatus.Suspended;
    }

    public void Close()
    {
        Status = ClientStatus.Closed;
    }
}

