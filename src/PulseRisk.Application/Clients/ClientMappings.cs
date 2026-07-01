using PulseRisk.Domain.Entities;

namespace PulseRisk.Application.Clients;

internal static class ClientMappings
{
    public static ClientDto ToDto(this Client client)
    {
        return new ClientDto(
            client.Id,
            client.Name,
            client.Status,
            client.CreatedAt);
    }
}

