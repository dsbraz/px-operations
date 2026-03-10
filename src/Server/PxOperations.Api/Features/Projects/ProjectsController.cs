using Microsoft.AspNetCore.Mvc;
using PxOperations.Application.Projects;
using PxOperations.Application.Projects.UseCases;
using PxOperations.Domain.Exceptions;

namespace PxOperations.Api.Features.Projects;

[ApiController]
[Route("api/projects")]
public sealed class ProjectsController(
    CreateProjectUseCase createProject,
    UpdateProjectUseCase updateProject,
    DeleteProjectUseCase deleteProject,
    GetProjectUseCase getProject,
    ListProjectsUseCase listProjects) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] string? dc,
        [FromQuery] string? status,
        [FromQuery] string? type,
        [FromQuery] string? renewal,
        CancellationToken ct)
    {
        var filter = new ProjectFilter(
            Search: search,
            Dc: dc is not null ? ProjectMappings.ParseDeliveryCenter(dc) : null,
            Status: status is not null ? ProjectMappings.ParseProjectStatus(status) : null,
            Type: type is not null ? ProjectMappings.ParseProjectType(type) : null,
            Renewal: renewal is not null ? ProjectMappings.ParseRenewalStatus(renewal) : null);

        var projects = await listProjects.ExecuteAsync(filter, ct);
        return Ok(projects.Select(ProjectMappings.ToResponse));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> GetById(int id, CancellationToken ct)
    {
        var project = await getProject.ExecuteAsync(id, ct);
        if (project is null) return NotFound();
        return Ok(ProjectMappings.ToResponse(project));
    }

    [HttpPost]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectResponse>> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken ct)
    {
        try
        {
            var command = new CreateProjectCommand(
                ProjectMappings.ParseDeliveryCenter(request.Dc),
                ProjectMappings.ParseProjectStatus(request.Status),
                request.Name,
                request.Client,
                ProjectMappings.ParseProjectType(request.Type),
                request.StartDate is not null ? DateOnly.Parse(request.StartDate) : null,
                request.EndDate is not null ? DateOnly.Parse(request.EndDate) : null,
                request.DeliveryManager,
                ProjectMappings.ParseRenewalStatus(request.Renewal),
                request.RenewalObservation);

            var project = await createProject.ExecuteAsync(command, ct);
            var response = ProjectMappings.ToResponse(project);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, response);
        }
        catch (BusinessRuleValidationException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpPatch("{id:int}")]
    [ProducesResponseType<ProjectResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectResponse>> Update(
        int id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken ct)
    {
        try
        {
            var command = new UpdateProjectCommand(
                Dc: request.Dc is not null ? ProjectMappings.ParseDeliveryCenter(request.Dc) : null,
                Status: request.Status is not null ? ProjectMappings.ParseProjectStatus(request.Status) : null,
                Name: request.Name,
                Client: request.Client,
                Type: request.Type is not null ? ProjectMappings.ParseProjectType(request.Type) : null,
                StartDate: request.StartDate is not null ? DateOnly.Parse(request.StartDate) : null,
                EndDate: request.EndDate is not null ? DateOnly.Parse(request.EndDate) : null,
                DeliveryManager: request.DeliveryManager,
                Renewal: request.Renewal is not null ? ProjectMappings.ParseRenewalStatus(request.Renewal) : null,
                RenewalObservation: request.RenewalObservation);

            var project = await updateProject.ExecuteAsync(id, command, ct);
            if (project is null) return NotFound();
            return Ok(ProjectMappings.ToResponse(project));
        }
        catch (BusinessRuleValidationException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await deleteProject.ExecuteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
