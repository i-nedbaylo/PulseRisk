using Microsoft.AspNetCore.Mvc;
using PulseRisk.Application.Quotes;

namespace PulseRisk.Api.Controllers;

[ApiController]
[Route("api/quotes")]
public sealed class QuotesController : ControllerBase
{
    [HttpGet("latest")]
    [ProducesResponseType<IReadOnlyCollection<LatestQuoteDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<LatestQuoteDto>>> GetLatest(
        [FromQuery] string? symbol,
        [FromServices] GetLatestQuotesHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetLatestQuotesQuery(QuerySymbols.ParseOptional(symbol));

        return Ok(await handler.HandleAsync(query, cancellationToken));
    }
}
