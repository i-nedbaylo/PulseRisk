using PulseRisk.Domain.Enums;

namespace PulseRisk.Application.Clients;

public sealed record ClientDto(
    Guid Id,
    string Name,
    ClientStatus Status,
    DateTimeOffset CreatedAt);

