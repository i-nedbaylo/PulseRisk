using Microsoft.AspNetCore.Mvc;
using PulseRisk.Application.Positions;

namespace PulseRisk.Api.Controllers;

[ApiController]
[Route("api/positions")]
public sealed class PositionsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<PositionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PositionDto>>> Get(
        [FromQuery] Guid? clientId,
        [FromQuery] Guid? tradingAccountId,
        [FromQuery] string? symbol,
        [FromQuery] bool openOnly,
        [FromServices] GetPositionsHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetPositionsQuery(
            clientId,
            tradingAccountId,
            QuerySymbols.ParseOptional(symbol),
            openOnly);

        return Ok(await handler.HandleAsync(query, cancellationToken));
    }

    [HttpGet("/api/clients/{clientId:guid}/positions")]
    [ProducesResponseType<IReadOnlyCollection<PositionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<PositionDto>>> GetByClientId(
        Guid clientId,
        [FromQuery] Guid? tradingAccountId,
        [FromQuery] string? symbol,
        [FromQuery] bool openOnly,
        [FromServices] GetPositionsHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetPositionsQuery(
            clientId,
            tradingAccountId,
            QuerySymbols.ParseOptional(symbol),
            openOnly);

        return Ok(await handler.HandleAsync(query, cancellationToken));
    }
}
