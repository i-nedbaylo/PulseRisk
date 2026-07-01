using Microsoft.AspNetCore.Mvc;
using PulseRisk.Application.Accounts;

namespace PulseRisk.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<TradingAccountDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TradingAccountDto>> Create(
        [FromBody] CreateTradingAccountCommand command,
        [FromServices] CreateTradingAccountHandler handler,
        CancellationToken cancellationToken)
    {
        var account = await handler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TradingAccountDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TradingAccountDto>> GetById(
        Guid id,
        [FromServices] GetTradingAccountByIdHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(id, cancellationToken));
    }

    [HttpGet("/api/clients/{clientId:guid}/accounts")]
    [ProducesResponseType<IReadOnlyCollection<TradingAccountDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TradingAccountDto>>> GetByClientId(
        Guid clientId,
        [FromServices] GetClientAccountsHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(clientId, cancellationToken));
    }
}

