using Microsoft.AspNetCore.Mvc;
using PxOperations.Api.Features.Milestones.Contracts;
using PxOperations.Application.Features.Milestones;
using PxOperations.Application.Features.Milestones.UseCases;
using PxOperations.Domain.Exceptions;

namespace PxOperations.Api.Features.Milestones;

[ApiController]
[Route("api/milestones")]
public sealed class MilestonesController(
    CreateMilestoneUseCase createMilestone,
    UpdateMilestoneUseCase updateMilestone,
    DeleteMilestoneUseCase deleteMilestone,
    GetMilestoneUseCase getMilestone,
    ListMilestonesUseCase listMilestones) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MilestoneResponse>>> List(
        [FromQuery] string? search,
        [FromQuery] string? dc,
        [FromQuery] string? type,
        [FromQuery] int? projectId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken ct)
    {
        var filter = new MilestoneFilter(
            search,
            MilestoneMappings.ParseDeliveryCenterOrNull(dc),
            type is not null ? MilestoneMappings.ParseMilestoneType(type) : null,
            projectId,
            from is not null ? DateOnly.Parse(from) : null,
            to is not null ? DateOnly.Parse(to) : null);

        var milestones = await listMilestones.ExecuteAsync(filter, ct);
        return Ok(milestones.Select(MilestoneMappings.ToResponse));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<MilestoneResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MilestoneResponse>> GetById(int id, CancellationToken ct)
    {
        var milestone = await getMilestone.ExecuteAsync(id, ct);
        if (milestone is null) return NotFound();
        return Ok(MilestoneMappings.ToResponse(milestone));
    }

    [HttpPost]
    [ProducesResponseType<MilestoneResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MilestoneResponse>> Create(
        [FromBody] CreateMilestoneRequest request,
        CancellationToken ct)
    {
        try
        {
            var milestone = await createMilestone.ExecuteAsync(
                new CreateMilestoneCommand(
                    request.ProjectId,
                    MilestoneMappings.ParseMilestoneType(request.Type),
                    request.Title,
                    DateOnly.Parse(request.Date),
                    request.Time is not null ? TimeOnly.Parse(request.Time) : null,
                    request.Notes),
                ct);

            var response = await getMilestone.ExecuteAsync(milestone.Id, ct);
            return CreatedAtAction(nameof(GetById), new { id = milestone.Id }, MilestoneMappings.ToResponse(response!));
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
    [ProducesResponseType<MilestoneResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MilestoneResponse>> Update(
        int id,
        [FromBody] UpdateMilestoneRequest request,
        CancellationToken ct)
    {
        try
        {
            var milestone = await updateMilestone.ExecuteAsync(
                id,
                new UpdateMilestoneCommand(
                    request.ProjectId,
                    request.Type.Map(MilestoneMappings.ParseMilestoneType),
                    request.Title,
                    request.Date.Map(DateOnly.Parse),
                    request.Time.Map(s => s is not null ? TimeOnly.Parse(s) : (TimeOnly?)null),
                    request.Notes),
                ct);

            if (milestone is null) return NotFound();

            var response = await getMilestone.ExecuteAsync(milestone.Id, ct);
            return Ok(MilestoneMappings.ToResponse(response!));
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
        var deleted = await deleteMilestone.ExecuteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
