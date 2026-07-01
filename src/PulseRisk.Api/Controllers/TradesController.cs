using Microsoft.AspNetCore.Mvc;
using PulseRisk.Application.Trades;

namespace PulseRisk.Api.Controllers;

[ApiController]
[Route("api/trades")]
public sealed class TradesController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<TradeDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TradeDto>> Create(
        [FromBody] CreateTradeCommand command,
        [FromServices] CreateTradeHandler handler,
        CancellationToken cancellationToken)
    {
        var trade = await handler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = trade.Id }, trade);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TradeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TradeDto>> GetById(
        Guid id,
        [FromServices] GetTradeByIdHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(id, cancellationToken));
    }
}
