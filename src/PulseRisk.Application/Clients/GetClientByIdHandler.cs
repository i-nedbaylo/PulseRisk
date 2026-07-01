using PulseRisk.Application.Common;
using PulseRisk.Application.Repositories;

namespace PulseRisk.Application.Clients;

public sealed class GetClientByIdHandler(IClientRepository clients)
{
    public async Task<ClientDto> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var client = await clients.GetByIdAsync(id, cancellationToken)
            ?? throw new EntityNotFoundException("Client", id.ToString());

        return client.ToDto();
    }
}

