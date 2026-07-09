using Microsoft.AspNetCore.Mvc;
using PulseRisk.Application.Common;
using PulseRisk.Application.Trades;
using PulseRisk.Domain.Enums;

namespace PulseRisk.Api.Controllers;

[ApiController]
[Route("api/trades")]
public sealed class TradesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<TradeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TradeDto>>> Get(
        [FromQuery] Guid? clientId,
        [FromQuery] Guid? tradingAccountId,
        [FromQuery] string? symbol,
        [FromQuery] TradeSide? side,
        [FromQuery] DateTimeOffset? createdFrom,
        [FromQuery] DateTimeOffset? createdTo,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromServices] GetTradesHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetTradesQuery(
            clientId,
            tradingAccountId,
            QuerySymbols.ParseOptional(symbol),
            side,
            createdFrom,
            createdTo,
            pageNumber,
            pageSize);

        return Ok(await handler.HandleAsync(query, cancellationToken));
    }

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

    [HttpGet("/api/clients/{clientId:guid}/trades")]
    [ProducesResponseType<PagedResult<TradeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TradeDto>>> GetByClientId(
        Guid clientId,
        [FromQuery] Guid? tradingAccountId,
        [FromQuery] string? symbol,
        [FromQuery] TradeSide? side,
        [FromQuery] DateTimeOffset? createdFrom,
        [FromQuery] DateTimeOffset? createdTo,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromServices] GetTradesHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetTradesQuery(
            clientId,
            tradingAccountId,
            QuerySymbols.ParseOptional(symbol),
            side,
            createdFrom,
            createdTo,
            pageNumber,
            pageSize);

        return Ok(await handler.HandleAsync(query, cancellationToken));
    }
}
