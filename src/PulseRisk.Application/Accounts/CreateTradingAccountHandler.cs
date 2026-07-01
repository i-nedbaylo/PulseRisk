using FluentValidation;
using PulseRisk.Application.Common;
using PulseRisk.Application.Repositories;
using PulseRisk.Domain.Entities;
using PulseRisk.Domain.ValueObjects;

namespace PulseRisk.Application.Accounts;

public sealed class CreateTradingAccountHandler(
    IValidator<CreateTradingAccountCommand> validator,
    IClientRepository clients,
    ITradingAccountRepository accounts,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<TradingAccountDto> HandleAsync(
        CreateTradingAccountCommand command,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        if (await clients.GetByIdAsync(command.ClientId, cancellationToken) is null)
        {
            throw new EntityNotFoundException("Client", command.ClientId.ToString());
        }

        var account = new TradingAccount(
            Guid.NewGuid(),
            command.ClientId,
            new Money(command.Balance, command.Currency),
            command.Leverage,
            timeProvider.GetUtcNow());

        await accounts.AddAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return account.ToDto();
    }
}

