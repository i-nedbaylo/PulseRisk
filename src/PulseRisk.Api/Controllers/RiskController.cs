using Microsoft.AspNetCore.Mvc;
using PulseRisk.Application.Common;
using PulseRisk.Application.Risk;
using PulseRisk.Domain.Enums;

namespace PulseRisk.Api.Controllers;

[ApiController]
[Route("api/risk")]
public sealed class RiskController : ControllerBase
{
    [HttpGet("clients/{clientId:guid}")]
    [ProducesResponseType<IReadOnlyCollection<ClientRiskMetricDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ClientRiskMetricDto>>> GetClientMetrics(
        Guid clientId,
        [FromServices] GetClientRiskMetricsHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(
            new GetClientRiskMetricsQuery(clientId),
            cancellationToken));
    }

    [HttpGet("alerts")]
    [ProducesResponseType<PagedResult<RiskAlertDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RiskAlertDto>>> GetAlerts(
        [FromQuery] Guid? clientId,
        [FromQuery] Guid? tradingAccountId,
        [FromQuery] string? symbol,
        [FromQuery] RiskAlertType? alertType,
        [FromQuery] RiskSeverity? severity,
        [FromQuery] bool activeOnly,
        [FromQuery] DateTimeOffset? createdFrom,
        [FromQuery] DateTimeOffset? createdTo,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromServices] GetRiskAlertsHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetRiskAlertsQuery(
            clientId,
            tradingAccountId,
            QuerySymbols.ParseOptional(symbol),
            alertType,
            severity,
            activeOnly,
            createdFrom,
            createdTo,
            pageNumber,
            pageSize);

        return Ok(await handler.HandleAsync(query, cancellationToken));
    }
}
