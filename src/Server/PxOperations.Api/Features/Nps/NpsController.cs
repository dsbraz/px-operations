using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    SubmitNpsPublicResponseUseCase submitPublicResponse,
    DismissNpsCollectionUseCase dismissCollection,
    ReactivateNpsCollectionUseCase reactivateCollection,
    GetNpsFilterOptionsUseCase getFilterOptions) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<NpsDashboardResponse>> GetDashboard(
        [FromQuery] string? search,
        [FromQuery] string[]? company,
        [FromQuery] string[]? dc,
        [FromQuery] string[]? deliveryManager,
        [FromQuery] string[]? projectType,
        [FromQuery] string[]? status,
        [FromQuery] int? projectId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string[]? classification,
        [FromQuery] string[]? format,
        // F6: os KPIs têm de ver a MESMA carteira que a tabela logo abaixo. Sem
        // isto o projeto dispensado sumia da lista e continuava somando no NPS.
        [FromQuery] bool includeDismissed,
        CancellationToken ct)
    {
        try
        {
            var dashboard = await getDashboard.ExecuteAsync(BuildFilter(search, company, dc, deliveryManager, projectType, status, projectId, from, to, classification, format, includeDismissed), ct);
            return Ok(NpsMappings.ToResponse(dashboard));
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or FormatException)
        {
            // FormatException vem de DateOnly.Parse em from/to. Sem ela na lista,
            // ?from=abc devolvia 500 — erro do cliente contado como falha nossa.
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpGet("projects")]
    public async Task<ActionResult<IEnumerable<NpsProjectResponse>>> ListProjects(
        [FromQuery] string? search,
        [FromQuery] string[]? company,
        [FromQuery] string[]? dc,
        [FromQuery] string[]? deliveryManager,
        [FromQuery] string[]? projectType,
        [FromQuery] string[]? status,
        [FromQuery] string? from,
        [FromQuery] string? to,
        // F1: "coletas dispensadas" é toggle Ocultar/Mostrar, não faceta de lista.
        [FromQuery] bool includeDismissed,
        CancellationToken ct)
    {
        try
        {
            var projects = await listProjects.ExecuteAsync(
                BuildFilter(search, company, dc, deliveryManager, projectType, status, null, from, to, null, null, includeDismissed), ct);
            return Ok(projects.Select(NpsMappings.ToResponse));
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or FormatException)
        {
            // FormatException vem de DateOnly.Parse em from/to. Sem ela na lista,
            // ?from=abc devolvia 500 — erro do cliente contado como falha nossa.
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// F1: empresa e DM são texto livre, então o menu de filtros precisa saber
    /// quais valores existem. Não sai da lista de projetos porque ela já vem
    /// filtrada — as opções encolheriam conforme o usuário filtra.
    /// </summary>
    [HttpGet("filter-options")]
    [ProducesResponseType<NpsFilterOptionsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NpsFilterOptionsResponse>> GetFilterOptions(CancellationToken ct)
        => Ok(NpsMappings.ToResponse(await getFilterOptions.ExecuteAsync(ct)));

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
    public async Task<ActionResult<IEnumerable<NpsSurveyResponse>>> ListDispatchResponses(int id, CancellationToken ct)
    {
        var responses = await listResponses.ExecuteAsync(id, NpsFilter.None, ct);
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

    /// <summary>
    /// B6: a listagem de respostas em JSON. A consulta já existia e alimentava
    /// o CSV; sem o endpoint, nem a tabela de auditoria (F10) nem o drill-down
    /// por projeto (F8) têm fonte de dados.
    ///
    /// Sem paginação de propósito: o PRD não pede, e o CSV já devolve a
    /// carteira inteira pelo mesmo caminho. Se o volume crescer, o lugar de
    /// paginar é aqui e no CSV juntos.
    /// </summary>
    [HttpGet("responses")]
    [ProducesResponseType<IEnumerable<NpsSurveyResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<NpsSurveyResponse>>> ListResponses(
        [FromQuery] string? search,
        [FromQuery] string[]? company,
        [FromQuery] string[]? dc,
        [FromQuery] string[]? deliveryManager,
        [FromQuery] string[]? projectType,
        [FromQuery] string[]? status,
        [FromQuery] int? projectId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string[]? classification,
        [FromQuery] string[]? format,
        // F6: o dispensado sai da lista e dos KPIs; as respostas dele têm de
        // sair junto, ou o CSV desmente a tela.
        [FromQuery] bool includeDismissed,
        CancellationToken ct)
    {
        try
        {
            var responses = await listResponses.ExecuteAsync(
                null, BuildFilter(search, company, dc, deliveryManager, projectType, status, projectId, from, to, classification, format, includeDismissed), ct);
            return Ok(responses.Select(NpsMappings.ToResponse));
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or FormatException)
        {
            // FormatException vem de DateOnly.Parse em from/to. Sem ela na lista,
            // ?from=abc devolvia 500 — erro do cliente contado como falha nossa.
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    [HttpGet("responses/export")]
    public async Task<IActionResult> ExportResponses(
        [FromQuery] string? search,
        [FromQuery] string[]? company,
        [FromQuery] string[]? dc,
        [FromQuery] string[]? deliveryManager,
        [FromQuery] string[]? projectType,
        [FromQuery] string[]? status,
        [FromQuery] int? projectId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string[]? classification,
        [FromQuery] string[]? format,
        // F6: o dispensado sai da lista e dos KPIs; as respostas dele têm de
        // sair junto, ou o CSV desmente a tela.
        [FromQuery] bool includeDismissed,
        CancellationToken ct)
    {
        try
        {
            var responses = await listResponses.ExecuteAsync(null, BuildFilter(search, company, dc, deliveryManager, projectType, status, projectId, from, to, classification, format, includeDismissed), ct);
            var csv = BuildCsv(responses);
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "nps-responses.csv");
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or FormatException)
        {
            // FormatException vem de DateOnly.Parse em from/to. Sem ela na lista,
            // ?from=abc devolvia 500 — erro do cliente contado como falha nossa.
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// F6: dispensar a coleta de um projeto, com motivo. Idempotente — dispensar
    /// duas vezes é ruído, não fato novo.
    /// </summary>
    [HttpPost("projects/{projectId:int}/collection-waiver")]
    [ProducesResponseType<NpsProjectResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NpsProjectResponse>> DismissCollection(
        int projectId, [FromBody] DismissNpsCollectionRequest request, CancellationToken ct)
    {
        try
        {
            var project = await dismissCollection.ExecuteAsync(projectId, new DismissNpsCollectionCommand(request.Reason), ct);
            return project is null ? NotFound() : Ok(NpsMappings.ToResponse(project));
        }
        catch (BusinessRuleValidationException ex)
        {
            return BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    /// <summary>
    /// F6: a volta atrás é parte do fluxo — reativar devolve o projeto à coluna
    /// que a regra indicar, sem perder histórico de respostas.
    /// </summary>
    [HttpDelete("projects/{projectId:int}/collection-waiver")]
    [ProducesResponseType<NpsProjectResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NpsProjectResponse>> ReactivateCollection(int projectId, CancellationToken ct)
    {
        var project = await reactivateCollection.ExecuteAsync(projectId, ct);
        return project is null ? NotFound() : Ok(NpsMappings.ToResponse(project));
    }

    [HttpGet("public/{token:guid}")]
    [ProducesResponseType<NpsPublicSurveyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NpsPublicSurveyResponse>> GetPublic(Guid token, CancellationToken ct)
    {
        var survey = await getPublicSurvey.ExecuteAsync(token, ct);
        return survey is null ? NotFound() : Ok(NpsMappings.ToResponse(survey));
    }

    /// <summary>
    /// B4: único endpoint com limite por IP. É o que fica exposto sem
    /// autenticação atrás de um link que qualquer um pode repassar.
    /// </summary>
    [HttpPost("public/{token:guid}/responses")]
    [EnableRateLimiting(AntiAbuse.SubmitPolicy)]
    [ProducesResponseType<NpsSurveyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<NpsSurveyResponse>> SubmitPublic(Guid token, SubmitNpsSurveyResponseRequest request, CancellationToken ct)
    {
        try
        {
            var response = await submitPublicResponse.ExecuteAsync(token, new SubmitNpsPublicResponseCommand(
                request.Score,
                request.BusinessValue,
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

    /// <summary>
    /// D11: as facetas de lista chegam como parâmetro repetido
    /// (?dc=DC1&amp;dc=DC2) — o formato nativo do ASP.NET, sem parsing de CSV.
    /// Valor desconhecido lança e vira 400: filtrar calado pelo valor errado
    /// é pior do que recusar.
    /// </summary>
    private static NpsFilter BuildFilter(
        string? search,
        string[]? company,
        string[]? dc,
        string[]? deliveryManager,
        string[]? projectType,
        string[]? status,
        int? projectId,
        string? from,
        string? to,
        string[]? classification,
        string[]? format = null,
        bool includeDismissed = false)
        => new(
            search,
            NpsMappings.ParseFacet(company, v => v.Trim()),
            NpsMappings.ParseFacet(dc, NpsMappings.ParseDc),
            NpsMappings.ParseFacet(deliveryManager, v => v.Trim()),
            NpsMappings.ParseFacet(projectType, NpsMappings.ParseProjectType),
            NpsMappings.ParseFacet(status, NpsMappings.ParseCollectionStatus),
            projectId,
            string.IsNullOrWhiteSpace(from) ? null : DateOnly.Parse(from),
            string.IsNullOrWhiteSpace(to) ? null : DateOnly.Parse(to),
            NpsMappings.ParseFacet(classification, NpsMappings.ParseClassification),
            NpsMappings.ParseFacet(format, NpsMappings.ParseFormFormat),
            includeDismissed);

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
