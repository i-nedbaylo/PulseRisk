using Microsoft.AspNetCore.Mvc;
using PulseRisk.Application.Clients;

namespace PulseRisk.Api.Controllers;

[ApiController]
[Route("api/clients")]
public sealed class ClientsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ClientDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ClientDto>> Create(
        [FromBody] CreateClientCommand command,
        [FromServices] CreateClientHandler handler,
        CancellationToken cancellationToken)
    {
        var client = await handler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<ClientDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ClientDto>>> Get(
        [FromServices] GetClientsHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ClientDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ClientDto>> GetById(
        Guid id,
        [FromServices] GetClientByIdHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(id, cancellationToken));
    }
}

