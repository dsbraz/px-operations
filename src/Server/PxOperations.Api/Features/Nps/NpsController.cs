using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PxOperations.Application.Features.Nps;
using PxOperations.Application.Features.Nps.UseCases;
using PxOperations.Api.Features.Nps.Contracts;

namespace PxOperations.Api.Features.Nps;

[ApiController]
[Route("api/nps")]
public sealed class NpsController(
    INpsQueries queries,
    CreateNpsContactUseCase createContact,
    UpdateNpsContactUseCase updateContact,
    ArchiveNpsContactUseCase archiveContact,
    CreateNpsDispatchUseCase createDispatch,
    WaiveNpsCollectionUseCase waiveCollection,
    ReactivateNpsCollectionUseCase reactivateCollection,
    SubmitNpsPublicResponseUseCase submitResponse,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<NpsDashboardView>> GetDashboard([FromQuery] NpsQueryRequest request, CancellationToken ct)
        => Ok(await queries.GetDashboardAsync(NpsMappings.ToFilter(request), timeProvider.GetUtcNow(), ct));

    [HttpGet("filter-options")]
    public async Task<ActionResult<NpsFilterOptionsView>> GetFilterOptions(CancellationToken ct)
        => Ok(await queries.GetFilterOptionsAsync(ct));

    [HttpGet("project-results")]
    public async Task<ActionResult<IReadOnlyList<NpsProjectResultView>>> ListProjectResults(
        [FromQuery] NpsQueryRequest request,
        CancellationToken ct)
        => Ok(await queries.ListProjectResultsAsync(NpsMappings.ToFilter(request), ct));

    [HttpGet("responses")]
    public async Task<ActionResult<IReadOnlyList<NpsResponseView>>> ListResponses(
        [FromQuery] NpsQueryRequest request,
        CancellationToken ct)
        => Ok(await queries.ListResponsesAsync(NpsMappings.ToFilter(request), ct));

    [HttpGet("projects")]
    public async Task<ActionResult<IReadOnlyList<NpsProjectView>>> ListProjects(
        [FromQuery] NpsQueryRequest request,
        CancellationToken ct)
    {
        var collectionFilter = NpsMappings.ToFilter(request) with
        {
            Statuses = [],
            Formats = [],
            Classifications = [],
            From = null,
            To = null
        };
        return Ok(await queries.ListProjectsAsync(collectionFilter, timeProvider.GetUtcNow(), ct));
    }

    [HttpGet("projects/{id:int}")]
    [ProducesResponseType<NpsProjectDetailView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NpsProjectDetailView>> GetProject(int id, CancellationToken ct)
    {
        var project = await queries.GetProjectAsync(id, timeProvider.GetUtcNow(), ct);
        return project is null ? NotFoundProblem() : Ok(project);
    }

    [HttpGet("projects/{id:int}/responses")]
    public async Task<ActionResult<IReadOnlyList<NpsResponseView>>> ListProjectResponses(
        int id,
        [FromQuery] NpsQueryRequest request,
        CancellationToken ct)
        => Ok(await queries.ListProjectResponsesAsync(id, NpsMappings.ToFilter(request), ct));

    [HttpGet("projects/{projectId:int}/contacts")]
    public async Task<ActionResult<IReadOnlyList<NpsContactView>>> ListContacts(
        int projectId,
        [FromQuery] bool includeArchived,
        CancellationToken ct)
        => Ok(await queries.ListContactsAsync(projectId, includeArchived, ct));

    [HttpPost("projects/{projectId:int}/contacts")]
    [ProducesResponseType<NpsContactView>(StatusCodes.Status201Created)]
    public async Task<ActionResult<NpsContactView>> CreateContact(
        int projectId,
        CreateNpsContactRequest request,
        CancellationToken ct)
    {
        var id = await createContact.ExecuteAsync(new CreateNpsContactCommand(
            projectId,
            request.Name,
            request.Email,
            request.Role), ct);
        var contact = await queries.GetContactAsync(id, ct);
        return CreatedAtAction(nameof(ListContacts), new { projectId }, contact);
    }

    [HttpPatch("contacts/{id:int}")]
    public async Task<ActionResult<NpsContactView>> UpdateContact(
        int id,
        UpdateNpsContactRequest request,
        CancellationToken ct)
    {
        await updateContact.ExecuteAsync(new UpdateNpsContactCommand(id, request.Name, request.Email, request.Role), ct);
        return Ok(await queries.GetContactAsync(id, ct));
    }

    [HttpDelete("contacts/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteContact(int id, CancellationToken ct)
    {
        await archiveContact.ExecuteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("projects/{id:int}/dispatches")]
    public async Task<ActionResult<IReadOnlyList<NpsDispatchView>>> ListDispatches(int id, CancellationToken ct)
        => Ok(await queries.ListDispatchesAsync(id, timeProvider.GetUtcNow(), ct));

    [HttpGet("dispatches/{id:int}")]
    [ProducesResponseType<NpsDispatchDetailView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NpsDispatchDetailView>> GetDispatch(int id, CancellationToken ct)
    {
        var dispatch = await queries.GetDispatchAsync(id, timeProvider.GetUtcNow(), ct);
        return dispatch is null ? NotFoundProblem() : Ok(dispatch);
    }

    [HttpGet("dispatches/{id:int}/responses")]
    public async Task<ActionResult<IReadOnlyList<NpsResponseView>>> ListDispatchResponses(int id, CancellationToken ct)
        => Ok(await queries.ListDispatchResponsesAsync(id, ct));

    [HttpPost("dispatches")]
    [ProducesResponseType<NpsDispatchDetailView>(StatusCodes.Status201Created)]
    public async Task<ActionResult<NpsDispatchDetailView>> CreateDispatch(
        CreateNpsDispatchRequest request,
        CancellationToken ct)
    {
        var id = await createDispatch.ExecuteAsync(new CreateNpsDispatchCommand(
            request.ProjectId,
            request.Format,
            request.Language,
            request.ContactIds ?? []), ct);
        var dispatch = await queries.GetDispatchAsync(id, timeProvider.GetUtcNow(), ct);
        return CreatedAtAction(nameof(GetDispatch), new { id }, dispatch);
    }

    [HttpPost("projects/{id:int}/waiver")]
    [ProducesResponseType<NpsProjectDetailView>(StatusCodes.Status201Created)]
    public async Task<ActionResult<NpsProjectDetailView>> WaiveCollection(
        int id,
        WaiveNpsCollectionRequest request,
        CancellationToken ct)
    {
        await waiveCollection.ExecuteAsync(new WaiveNpsCollectionCommand(id, request.Reason), ct);
        var project = await queries.GetProjectAsync(id, timeProvider.GetUtcNow(), ct);
        return CreatedAtAction(nameof(GetProject), new { id }, project);
    }

    [HttpDelete("projects/{id:int}/waiver")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReactivateCollection(int id, CancellationToken ct)
    {
        await reactivateCollection.ExecuteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("public/{token:guid}")]
    [ProducesResponseType<NpsPublicSurveyView>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NpsPublicSurveyView>> GetPublic(Guid token, CancellationToken ct)
    {
        var survey = await queries.GetPublicSurveyAsync(token, timeProvider.GetUtcNow(), ct);
        return survey is null ? NotFoundProblem() : Ok(survey);
    }

    [HttpPost("public/{token:guid}/responses")]
    [EnableRateLimiting("nps-public")]
    [ProducesResponseType<NpsResponseView>(StatusCodes.Status201Created)]
    public async Task<ActionResult<NpsResponseView>> SubmitPublic(
        Guid token,
        SubmitNpsSurveyResponseRequest request,
        CancellationToken ct)
    {
        var id = await submitResponse.ExecuteAsync(new SubmitNpsPublicResponseCommand(
            token,
            request.Score,
            request.Quality,
            request.Schedule,
            request.Communication,
            request.BusinessValue,
            request.Comment,
            request.RespondentName,
            request.RespondentEmail), ct);
        var response = await queries.GetResponseAsync(id, ct);
        return CreatedAtAction(nameof(GetPublic), new { token }, response);
    }

    [HttpGet("responses/export")]
    public async Task<IActionResult> ExportResponses([FromQuery] NpsQueryRequest request, CancellationToken ct)
    {
        var responses = await queries.ListResponsesAsync(NpsMappings.ToFilter(request), ct);
        return File(
            Encoding.UTF8.GetBytes(NpsResponsesCsv.Build(responses)),
            "text/csv; charset=utf-8",
            "nps-responses.csv");
    }

    private ActionResult NotFoundProblem()
        => Problem(statusCode: StatusCodes.Status404NotFound, title: "Resource not found");
}
