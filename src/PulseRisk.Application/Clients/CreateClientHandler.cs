using FluentValidation;
using PulseRisk.Application.Common;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Entities;

namespace PulseRisk.Application.Clients;

public sealed class CreateClientHandler(
    IValidator<CreateClientCommand> validator,
    IClientRepository clients,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ClientDto> HandleAsync(
        CreateClientCommand command,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var client = Client.Create(command.Name, timeProvider.GetUtcNow());

        await clients.AddAsync(client, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return client.ToDto();
    }
}

