using PulseRisk.Application.Repositories;

namespace PulseRisk.Application.Clients;

public sealed class GetClientsHandler(IClientRepository clients)
{
    public async Task<IReadOnlyCollection<ClientDto>> HandleAsync(CancellationToken cancellationToken)
    {
        var result = await clients.ListAsync(cancellationToken);

        return result.Select(client => client.ToDto()).ToArray();
    }
}

