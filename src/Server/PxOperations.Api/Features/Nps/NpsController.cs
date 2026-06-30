using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using PxOperations.Api.Features.Nps.Contracts;
using PxOperations.Application.Features.Nps;
using PxOperations.Application.Features.Nps.UseCases;
using PxOperations.Domain.Exceptions;

namespace PxOperations.Api.Features.Nps;

[ApiController]
[Route("api/nps")]
public sealed class NpsController(
    GetNpsDashboardUseCase getDashboard,
    ListNpsProjectsUseCase listProjects,
    GetNpsProjectUseCase getProject,
    ListNpsContactsUseCase listContacts,
    CreateNpsContactUseCase createContact,
    UpdateNpsContactUseCase updateContact,
    DeleteNpsContactUseCase deleteContact,
    ListNpsDispatchesUseCase listDispatches,
    CreateNpsDispatchUseCase createDispatch,
    GetNpsDispatchUseCase getDispatch,
    ListNpsResponsesUseCase listResponses,
    CloseNpsDispatchUseCase closeDispatch,
    GetNpsPublicSurveyUseCase getPublicSurvey,
    SubmitNpsPublicResponseUseCase submitPublicResponse) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<NpsDashboardResponse>> GetDashboard(
        [FromQuery] string? search,
        [FromQuery] string? dc,
        [FromQuery] string? deliveryManager,
        [FromQuery] string? projectType,
        [FromQuery] int? projectId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? classification,
        CancellationToken ct)
    {
        try
        {
            var dashboard = await getDashboard.ExecuteAsync(BuildFilter(search, dc, deliveryManager, projectType, projectId, from, to, classification), ct);
            return Ok(NpsMappings.ToResponse(dashboard));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpGet("projects")]
    public async Task<ActionResult<IEnumerable<NpsProjectResponse>>> ListProjects(
        [FromQuery] string? search,
        [FromQuery] string? dc,
        [FromQuery] string? deliveryManager,
        [FromQuery] string? projectType,
        CancellationToken ct)
    {
        var projects = await listProjects.ExecuteAsync(new NpsFilter(search, dc, deliveryManager, projectType, null, null, null, null), ct);
        return Ok(projects.Select(NpsMappings.ToResponse));
    }

    [HttpGet("projects/{projectId:int}")]
    [ProducesResponseType<NpsProjectDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NpsProjectDetailResponse>> GetProject(int projectId, CancellationToken ct)
    {
        var project = await getProject.ExecuteAsync(projectId, ct);
        return project is null ? NotFound() : Ok(NpsMappings.ToResponse(project));
    }

    [HttpGet("projects/{projectId:int}/contacts")]
    public async Task<ActionResult<IEnumerable<NpsContactResponse>>> ListContacts(
        int projectId,
        [FromQuery] bool includeArchived,
        CancellationToken ct)
    {
        var contacts = await listContacts.ExecuteAsync(projectId, includeArchived, ct);
        return Ok(contacts.Select(NpsMappings.ToResponse));
    }

    [HttpPost("projects/{projectId:int}/contacts")]
    [ProducesResponseType<NpsContactResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NpsContactResponse>> CreateContact(int projectId, CreateNpsContactRequest request, CancellationToken ct)
    {
        try
        {
            var contact = await createContact.ExecuteAsync(projectId, new CreateNpsContactCommand(request.Name, request.Email, request.Role), ct);
            return CreatedAtAction(nameof(ListContacts), new { projectId }, NpsMappings.ToResponse(contact));
        }
        catch (Exception ex) when (ex is BusinessRuleValidationException or KeyNotFoundException)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpPatch("contacts/{id:int}")]
    [ProducesResponseType<NpsContactResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NpsContactResponse>> UpdateContact(int id, UpdateNpsContactRequest request, CancellationToken ct)
    {
        try
        {
            var contact = await updateContact.ExecuteAsync(id, new UpdateNpsContactCommand(request.Name, request.Email, request.Role), ct);
            return contact is null ? NotFound() : Ok(NpsMappings.ToResponse(contact));
        }
        catch (BusinessRuleValidationException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpDelete("contacts/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContact(int id, CancellationToken ct)
    {
        var deleted = await deleteContact.ExecuteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("projects/{projectId:int}/dispatches")]
    public async Task<ActionResult<IEnumerable<NpsDispatchResponse>>> ListDispatches(int projectId, CancellationToken ct)
    {
        var dispatches = await listDispatches.ExecuteAsync(projectId, ct);
        return Ok(dispatches.Select(NpsMappings.ToResponse));
    }

    [HttpPost("dispatches")]
    [ProducesResponseType<NpsDispatchDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NpsDispatchDetailResponse>> CreateDispatch(CreateNpsDispatchRequest request, CancellationToken ct)
    {
        try
        {
            var dispatch = await createDispatch.ExecuteAsync(new CreateNpsDispatchCommand(
                request.ProjectId,
                DateOnly.Parse(request.PeriodStart),
                DateOnly.Parse(request.PeriodEnd),
                NpsMappings.ParseFormFormat(request.Format),
                NpsMappings.ParseLanguage(request.Language),
                request.CreatedBy,
                request.ContactIds ?? [],
                request.CreateGenericToken), ct);

            return CreatedAtAction(nameof(GetDispatch), new { id = dispatch.Dispatch.Id }, NpsMappings.ToResponse(dispatch));
        }
        catch (Exception ex) when (ex is BusinessRuleValidationException or KeyNotFoundException or InvalidOperationException or ArgumentOutOfRangeException)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpGet("dispatches/{id:int}")]
    [ProducesResponseType<NpsDispatchDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NpsDispatchDetailResponse>> GetDispatch(int id, CancellationToken ct)
    {
        var dispatch = await getDispatch.ExecuteAsync(id, ct);
        return dispatch is null ? NotFound() : Ok(NpsMappings.ToResponse(dispatch));
    }

    [HttpGet("dispatches/{id:int}/responses")]
    public async Task<ActionResult<IEnumerable<NpsSurveyResponse>>> ListResponses(int id, CancellationToken ct)
    {
        var responses = await listResponses.ExecuteAsync(id, new NpsFilter(null, null, null, null, null, null, null, null), ct);
        return Ok(responses.Select(NpsMappings.ToResponse));
    }

    [HttpPatch("dispatches/{id:int}/close")]
    [ProducesResponseType<NpsDispatchDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NpsDispatchDetailResponse>> CloseDispatch(int id, CancellationToken ct)
    {
        var dispatch = await closeDispatch.ExecuteAsync(id, ct);
        return dispatch is null ? NotFound() : Ok(NpsMappings.ToResponse(dispatch));
    }

    [HttpGet("responses/export")]
    public async Task<IActionResult> ExportResponses(
        [FromQuery] string? search,
        [FromQuery] string? dc,
        [FromQuery] string? deliveryManager,
        [FromQuery] string? projectType,
        [FromQuery] int? projectId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? classification,
        CancellationToken ct)
    {
        var responses = await listResponses.ExecuteAsync(null, BuildFilter(search, dc, deliveryManager, projectType, projectId, from, to, classification), ct);
        var csv = BuildCsv(responses);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "nps-responses.csv");
    }

    [HttpGet("public/{token:guid}")]
    [ProducesResponseType<NpsPublicSurveyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NpsPublicSurveyResponse>> GetPublic(Guid token, CancellationToken ct)
    {
        var survey = await getPublicSurvey.ExecuteAsync(token, ct);
        return survey is null ? NotFound() : Ok(NpsMappings.ToResponse(survey));
    }

    [HttpPost("public/{token:guid}/responses")]
    [ProducesResponseType<NpsSurveyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<NpsSurveyResponse>> SubmitPublic(Guid token, SubmitNpsSurveyResponseRequest request, CancellationToken ct)
    {
        try
        {
            var response = await submitPublicResponse.ExecuteAsync(token, new SubmitNpsPublicResponseCommand(
                request.Score,
                request.Scope,
                request.Schedule,
                request.Quality,
                request.Communication,
                request.Tags,
                request.Comment,
                request.RespondentName,
                request.RespondentEmail), ct);

            return CreatedAtAction(nameof(GetPublic), new { token }, NpsMappings.ToResponse(response));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails { Detail = ex.Message });
        }
        catch (BusinessRuleValidationException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    private static NpsFilter BuildFilter(
        string? search,
        string? dc,
        string? deliveryManager,
        string? projectType,
        int? projectId,
        string? from,
        string? to,
        string? classification)
        => new(
            search,
            dc,
            deliveryManager,
            projectType,
            projectId,
            string.IsNullOrWhiteSpace(from) ? null : DateOnly.Parse(from),
            string.IsNullOrWhiteSpace(to) ? null : DateOnly.Parse(to),
            NpsMappings.ParseClassificationOrNull(classification));

    private static string BuildCsv(IEnumerable<NpsResponseView> responses)
    {
        var builder = new StringBuilder();
        builder.AppendLine("id,project_id,project_name,dispatch_id,score,classification,submitted_at,contact_email,respondent_email,comment");
        foreach (var response in responses)
        {
            builder.AppendLine(string.Join(',', [
                response.Id.ToString(),
                response.ProjectId.ToString(),
                Csv(response.ProjectName),
                response.DispatchId.ToString(),
                response.Score.ToString(),
                Csv(response.Classification),
                Csv(response.SubmittedAt),
                Csv(response.ContactEmail),
                Csv(response.RespondentEmail),
                Csv(response.Comment)
            ]));
        }

        return builder.ToString();
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
