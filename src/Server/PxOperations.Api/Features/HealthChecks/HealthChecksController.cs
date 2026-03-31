using Microsoft.AspNetCore.Mvc;
using PxOperations.Api.Features.HealthChecks.Contracts;
using PxOperations.Application.Features.HealthChecks;
using PxOperations.Application.Features.HealthChecks.UseCases;
using PxOperations.Domain.Exceptions;

namespace PxOperations.Api.Features.HealthChecks;

[ApiController]
[Route("api/health-checks")]
public sealed class HealthChecksController(
    CreateHealthCheckUseCase createHealthCheck,
    UpdateHealthCheckUseCase updateHealthCheck,
    DeleteHealthCheckUseCase deleteHealthCheck,
    GetHealthCheckUseCase getHealthCheck,
    ListHealthChecksUseCase listHealthChecks,
    GetHealthCheckSummaryUseCase getHealthCheckSummary) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HealthCheckResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] string? dc,
        [FromQuery] int? projectId,
        [FromQuery] string? week,
        [FromQuery] int? minScore,
        [FromQuery] int? maxScore,
        CancellationToken ct)
    {
        var filter = new HealthCheckFilter(
            search,
            HealthCheckMappings.ParseDeliveryCenterOrNull(dc),
            projectId,
            week is not null ? DateOnly.Parse(week) : null,
            minScore,
            maxScore);

        var healthChecks = await listHealthChecks.ExecuteAsync(filter, ct);
        return Ok(healthChecks.Select(HealthCheckMappings.ToResponse));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<HealthCheckSummaryResponse>> GetSummary(
        [FromQuery] string? search,
        [FromQuery] string? dc,
        [FromQuery] int? projectId,
        [FromQuery] string? week,
        [FromQuery] int? minScore,
        [FromQuery] int? maxScore,
        CancellationToken ct)
    {
        var filter = new HealthCheckFilter(
            search,
            HealthCheckMappings.ParseDeliveryCenterOrNull(dc),
            projectId,
            week is not null ? DateOnly.Parse(week) : null,
            minScore,
            maxScore);

        var summary = await getHealthCheckSummary.ExecuteAsync(filter, ct);
        return Ok(HealthCheckMappings.ToSummaryResponse(summary));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<HealthCheckResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HealthCheckResponse>> GetById(int id, CancellationToken ct)
    {
        var healthCheck = await getHealthCheck.ExecuteAsync(id, ct);
        if (healthCheck is null) return NotFound();
        return Ok(HealthCheckMappings.ToResponse(healthCheck));
    }

    [HttpPost]
    [ProducesResponseType<HealthCheckResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HealthCheckResponse>> Create(
        [FromBody] CreateHealthCheckRequest request,
        CancellationToken ct)
    {
        try
        {
            var healthCheck = await createHealthCheck.ExecuteAsync(
                new CreateHealthCheckCommand(
                    request.ProjectId,
                    request.SubProject,
                    DateOnly.Parse(request.Week),
                    request.ReporterEmail,
                    request.PracticesCount,
                    HealthCheckMappings.ParseRagStatus(request.Scope),
                    HealthCheckMappings.ParseRagStatus(request.Schedule),
                    HealthCheckMappings.ParseRagStatus(request.Quality),
                    HealthCheckMappings.ParseRagStatus(request.Satisfaction),
                    request.ExpansionOpportunity,
                    request.ExpansionComment,
                    request.ActionPlanNeeded,
                    request.Highlights),
                ct);

            var response = await getHealthCheck.ExecuteAsync(healthCheck.Id, ct);
            return CreatedAtAction(nameof(GetById), new { id = healthCheck.Id }, HealthCheckMappings.ToResponse(response!));
        }
        catch (BusinessRuleValidationException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpPatch("{id:int}")]
    [ProducesResponseType<HealthCheckResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HealthCheckResponse>> Update(
        int id,
        [FromBody] UpdateHealthCheckRequest request,
        CancellationToken ct)
    {
        try
        {
            var healthCheck = await updateHealthCheck.ExecuteAsync(
                id,
                new UpdateHealthCheckCommand(
                    request.ProjectId,
                    request.SubProject,
                    request.Week.Map(DateOnly.Parse),
                    request.ReporterEmail,
                    request.PracticesCount,
                    request.Scope.Map(HealthCheckMappings.ParseRagStatus),
                    request.Schedule.Map(HealthCheckMappings.ParseRagStatus),
                    request.Quality.Map(HealthCheckMappings.ParseRagStatus),
                    request.Satisfaction.Map(HealthCheckMappings.ParseRagStatus),
                    request.ExpansionOpportunity,
                    request.ExpansionComment,
                    request.ActionPlanNeeded,
                    request.Highlights),
                ct);

            if (healthCheck is null) return NotFound();

            var response = await getHealthCheck.ExecuteAsync(healthCheck.Id, ct);
            return Ok(HealthCheckMappings.ToResponse(response!));
        }
        catch (BusinessRuleValidationException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await deleteHealthCheck.ExecuteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
