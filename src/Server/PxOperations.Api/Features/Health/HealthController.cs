using Microsoft.AspNetCore.Mvc;
using PxOperations.Application.Features.Diagnostics;

namespace PxOperations.Api.Features.Health;

[ApiController]
[Route("health")]
public sealed class HealthController(IReadinessService readinessService) : ControllerBase
{
    [HttpGet("live")]
    public IActionResult Live()
    {
        return Ok();
    }

    [HttpGet("ready")]
    [ProducesResponseType<HealthStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<HealthStatusResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthStatusResponse>> Ready(CancellationToken cancellationToken)
    {
        var status = await readinessService.CheckAsync(cancellationToken);
        var response = new HealthStatusResponse(status.Status);

        return status.IsReady
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
