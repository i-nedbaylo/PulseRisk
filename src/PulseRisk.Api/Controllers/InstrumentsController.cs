using Microsoft.AspNetCore.Mvc;
using PulseRisk.Application.Instruments;

namespace PulseRisk.Api.Controllers;

[ApiController]
[Route("api/instruments")]
public sealed class InstrumentsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<InstrumentDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<InstrumentDto>> Create(
        [FromBody] CreateInstrumentCommand command,
        [FromServices] CreateInstrumentHandler handler,
        CancellationToken cancellationToken)
    {
        var instrument = await handler.HandleAsync(command, cancellationToken);

        return Created($"/api/instruments/{instrument.Symbol}", instrument);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<InstrumentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<InstrumentDto>>> Get(
        [FromServices] GetInstrumentsHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(cancellationToken));
    }
}

