using Microsoft.AspNetCore.Mvc;
using PxOperations.Api.Features.ProjectHealth.Contracts;
using PxOperations.Application.Features.ProjectHealth;
using PxOperations.Application.Features.ProjectHealth.UseCases;
using PxOperations.Domain.Exceptions;

namespace PxOperations.Api.Features.ProjectHealth;

[ApiController]
[Route("api/project-health")]
public sealed class ProjectHealthController(
    CreateProjectHealthUseCase createProjectHealth,
    UpdateProjectHealthUseCase updateProjectHealth,
    DeleteProjectHealthUseCase deleteProjectHealth,
    GetProjectHealthUseCase getProjectHealth,
    ListProjectHealthUseCase listProjectHealth,
    GetProjectHealthSummaryUseCase getProjectHealthSummary) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectHealthResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] string? dc,
        [FromQuery] int? projectId,
        [FromQuery] string? week,
        [FromQuery] int? minScore,
        [FromQuery] int? maxScore,
        CancellationToken ct)
    {
        var filter = new ProjectHealthFilter(
            search,
            ProjectHealthMappings.ParseDeliveryCenterOrNull(dc),
            projectId,
            week is not null ? DateOnly.Parse(week) : null,
            minScore,
            maxScore);

        var entries = await listProjectHealth.ExecuteAsync(filter, ct);
        return Ok(entries.Select(ProjectHealthMappings.ToResponse));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ProjectHealthSummaryResponse>> GetSummary(
        [FromQuery] string? search,
        [FromQuery] string? dc,
        [FromQuery] int? projectId,
        [FromQuery] string? week,
        [FromQuery] int? minScore,
        [FromQuery] int? maxScore,
        CancellationToken ct)
    {
        var filter = new ProjectHealthFilter(
            search,
            ProjectHealthMappings.ParseDeliveryCenterOrNull(dc),
            projectId,
            week is not null ? DateOnly.Parse(week) : null,
            minScore,
            maxScore);

        var summary = await getProjectHealthSummary.ExecuteAsync(filter, ct);
        return Ok(ProjectHealthMappings.ToSummaryResponse(summary));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProjectHealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectHealthResponse>> GetById(int id, CancellationToken ct)
    {
        var projectHealth = await getProjectHealth.ExecuteAsync(id, ct);
        if (projectHealth is null) return NotFound();
        return Ok(ProjectHealthMappings.ToResponse(projectHealth));
    }

    [HttpPost]
    [ProducesResponseType<ProjectHealthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectHealthResponse>> Create(
        [FromBody] CreateProjectHealthRequest request,
        CancellationToken ct)
    {
        try
        {
            var projectHealth = await createProjectHealth.ExecuteAsync(
                new CreateProjectHealthCommand(
                    request.ProjectId,
                    request.SubProject,
                    DateOnly.Parse(request.Week),
                    request.ReporterEmail,
                    request.PracticesCount,
                    ProjectHealthMappings.ParseRagStatus(request.Scope),
                    ProjectHealthMappings.ParseRagStatus(request.Schedule),
                    ProjectHealthMappings.ParseRagStatus(request.Quality),
                    ProjectHealthMappings.ParseRagStatus(request.Satisfaction),
                    request.ExpansionOpportunity,
                    request.ExpansionComment,
                    request.ActionPlanNeeded,
                    request.Highlights),
                ct);

            var response = await getProjectHealth.ExecuteAsync(projectHealth.Id, ct);
            return CreatedAtAction(nameof(GetById), new { id = projectHealth.Id }, ProjectHealthMappings.ToResponse(response!));
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
    [ProducesResponseType<ProjectHealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectHealthResponse>> Update(
        int id,
        [FromBody] UpdateProjectHealthRequest request,
        CancellationToken ct)
    {
        try
        {
            var projectHealth = await updateProjectHealth.ExecuteAsync(
                id,
                new UpdateProjectHealthCommand(
                    request.ProjectId,
                    request.SubProject,
                    request.Week.Map(DateOnly.Parse),
                    request.ReporterEmail,
                    request.PracticesCount,
                    request.Scope.Map(ProjectHealthMappings.ParseRagStatus),
                    request.Schedule.Map(ProjectHealthMappings.ParseRagStatus),
                    request.Quality.Map(ProjectHealthMappings.ParseRagStatus),
                    request.Satisfaction.Map(ProjectHealthMappings.ParseRagStatus),
                    request.ExpansionOpportunity,
                    request.ExpansionComment,
                    request.ActionPlanNeeded,
                    request.Highlights),
                ct);

            if (projectHealth is null) return NotFound();

            var response = await getProjectHealth.ExecuteAsync(projectHealth.Id, ct);
            return Ok(ProjectHealthMappings.ToResponse(response!));
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
        var deleted = await deleteProjectHealth.ExecuteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
