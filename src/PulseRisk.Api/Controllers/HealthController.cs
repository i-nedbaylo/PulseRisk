using Microsoft.AspNetCore.Mvc;

namespace PulseRisk.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get()
    {
        return Ok(new HealthResponse(
            Service: "PulseRisk.Api",
            Status: "Healthy",
            TimestampUtc: DateTimeOffset.UtcNow));
    }
}

public sealed record HealthResponse(
    string Service,
    string Status,
    DateTimeOffset TimestampUtc);

