using Microsoft.EntityFrameworkCore;
using Npgsql;
using PxOperations.Application.Features.Nps;
using PxOperations.Domain.Nps;
using PxOperations.Domain.Nps.Calculation;
using PxOperations.Domain.Projects;
using PxOperations.Infrastructure.Persistence;

namespace PxOperations.Infrastructure.Features.Nps;

public sealed class NpsRepository(AppDbContext dbContext) : INpsRepository
{
    public Task<bool> ProjectExistsAsync(int projectId, CancellationToken ct)
        => dbContext.Projects.AnyAsync(p => p.Id == projectId, ct);

    public Task<bool> ContactBelongsToProjectAsync(int projectId, int contactId, CancellationToken ct)
        => dbContext.Set<Contact>().AnyAsync(c => c.Id == contactId && c.ProjectId == projectId && !c.IsArchived, ct);

    public Task<CollectionWaiver?> GetActiveWaiverAsync(int projectId, CancellationToken ct)
        => dbContext.Set<CollectionWaiver>()
            .FirstOrDefaultAsync(w => w.ProjectId == projectId && w.ReactivatedAt == null, ct);

    public void AddWaiver(CollectionWaiver waiver) => dbContext.Set<CollectionWaiver>().Add(waiver);

    public bool IsDuplicateWaiverException(Exception exception)
        => exception is DbUpdateException
        {
            InnerException: PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_nps_collection_waivers_project_id"
            }
        };

    public void AddContact(Contact contact) => dbContext.Set<Contact>().Add(contact);

    public Task<Contact?> GetContactAsync(int id, CancellationToken ct)
        => dbContext.Set<Contact>().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<NpsContactView>> ListContactsAsync(int projectId, bool includeArchived, CancellationToken ct)
    {
        var query = dbContext.Set<Contact>().Where(c => c.ProjectId == projectId);
        if (!includeArchived)
        {
            query = query.Where(c => !c.IsArchived);
        }

        var contacts = await query.OrderBy(c => c.Name).ToListAsync(ct);
        return contacts.Select(ToContactView).ToList();
    }

    public async Task<NpsDashboardView> GetDashboardAsync(NpsFilter filter, CancellationToken ct)
    {
        var projectIds = await ApplyProjectFilters(dbContext.Projects.AsQueryable(), filter)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var responses = await ApplyResponseFilters(dbContext.Set<SurveyResponse>().Where(r => projectIds.Contains(r.ProjectId)), filter)
            .ToListAsync(ct);

        var activeDispatches = await dbContext.Set<Dispatch>()
            .CountAsync(d => projectIds.Contains(d.ProjectId) && d.Status == NpsDispatchStatus.Open, ct);

        var overdueProjects = await CountOverdueProjectsAsync(projectIds, ct);
        var classifications = responses.Select(r => r.Classification).ToList();
        var distribution = NpsCalculator.Distribution(classifications);

        return new NpsDashboardView(
            projectIds.Count,
            overdueProjects,
            activeDispatches,
            responses.Count,
            NpsCalculator.CalculateOfficialScore(classifications),
            responses.Count == 0 ? 0 : Math.Round((decimal)responses.Average(r => r.Score), 1),
            distribution[NpsClassification.Detractor],
            distribution[NpsClassification.Passive],
            distribution[NpsClassification.Promoter]);
    }

    public async Task<IReadOnlyList<NpsProjectView>> ListProjectsAsync(NpsFilter filter, CancellationToken ct)
    {
        var projects = await ApplyProjectFilters(dbContext.Projects.AsQueryable(), filter)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        var projectIds = projects.Select(p => p.Id).ToList();
        var contacts = await dbContext.Set<Contact>()
            .Where(c => projectIds.Contains(c.ProjectId) && !c.IsArchived)
            .GroupBy(c => c.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count, ct);

        // Contagem e prazo saem agregados no banco. Quando há mais de um link
        // aberto vale o que vence por último: é o que ainda dá para responder.
        var openDispatches = await dbContext.Set<Dispatch>()
            .Where(d => projectIds.Contains(d.ProjectId) && d.Status == NpsDispatchStatus.Open)
            .GroupBy(d => d.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count(), LastExpiry = g.Max(d => d.ExpiresAt) })
            .ToDictionaryAsync(x => x.ProjectId, x => x, ct);

        var activeDispatches = openDispatches.ToDictionary(kv => kv.Key, kv => kv.Value.Count);
        var dispatchExpiry = openDispatches.ToDictionary(kv => kv.Key, kv => kv.Value.LastExpiry);

        var linkTargets = await dbContext.Set<DispatchTarget>()
            .Where(t => projectIds.Contains(t.ProjectId))
            .GroupBy(t => t.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                Count = g.Count(),
                AnsweredCount = g.Count(t => t.Responses.Any())
            })
            .ToDictionaryAsync(x => x.ProjectId, x => x, ct);

        var responses = await ApplyResponseFilters(dbContext.Set<SurveyResponse>().Where(r => projectIds.Contains(r.ProjectId)), filter)
            .ToListAsync(ct);

        var lastResponses = responses
            .GroupBy(r => r.ProjectId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.SubmittedAt).First());

        var responseCounts = responses
            .GroupBy(r => r.ProjectId)
            .ToDictionary(g => g.Key, g => g.Count());

        var npsByProject = responses
            .GroupBy(r => r.ProjectId)
            .ToDictionary(g => g.Key, g => (decimal?)NpsCalculator.CalculateOfficialScore(g.Select(r => r.Classification)));

        var overdue = await GetOverdueProjectIdsAsync(projectIds, ct);

        var waivers = await dbContext.Set<CollectionWaiver>()
            .Where(w => projectIds.Contains(w.ProjectId) && w.ReactivatedAt == null)
            .ToDictionaryAsync(w => w.ProjectId, w => w, ct);

        // F6: dispensado sai do quadro por padrão; o toggle "Mostrar
        // dispensados" é quem os traz de volta.
        if (!filter.IncludeDismissed)
        {
            projects = projects.Where(p => !waivers.ContainsKey(p.Id)).ToList();
            projectIds = projects.Select(p => p.Id).ToList();
        }

        return projects.Select(project => ToProjectView(
            project,
            contacts.GetValueOrDefault(project.Id),
            activeDispatches.GetValueOrDefault(project.Id),
            linkTargets.GetValueOrDefault(project.Id)?.Count ?? 0,
            linkTargets.GetValueOrDefault(project.Id)?.AnsweredCount ?? 0,
            responseCounts.GetValueOrDefault(project.Id),
            lastResponses.GetValueOrDefault(project.Id),
            npsByProject.GetValueOrDefault(project.Id),
            overdue.Contains(project.Id),
            waivers.GetValueOrDefault(project.Id),
            dispatchExpiry.TryGetValue(project.Id, out var expiry) ? expiry : null)).ToList();
    }

    public async Task<NpsProjectDetailView?> GetProjectAsync(int projectId, CancellationToken ct)
    {
        var project = (await ListProjectsAsync(NpsFilter.ForProject(projectId) with { IncludeDismissed = true }, ct)).SingleOrDefault();
        if (project is null)
        {
            return null;
        }

        var contacts = await ListContactsAsync(projectId, includeArchived: true, ct);
        var dispatches = await ListDispatchesAsync(projectId, ct);
        var responses = await ListResponsesAsync(null, NpsFilter.ForProject(projectId) with { IncludeDismissed = true }, ct);

        return new NpsProjectDetailView(project, contacts, dispatches, responses.Take(20).ToList());
    }

    public void AddDispatch(Dispatch dispatch) => dbContext.Set<Dispatch>().Add(dispatch);

    public void AddDispatchTarget(DispatchTarget target) => dbContext.Set<DispatchTarget>().Add(target);

    public async Task<IReadOnlyList<NpsDispatchView>> ListDispatchesAsync(int projectId, CancellationToken ct)
    {
        var dispatches = await dbContext.Set<Dispatch>()
            .Include(d => d.Project)
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        return await ToDispatchViewsAsync(dispatches, ct);
    }

    public async Task<NpsDispatchDetailView?> GetDispatchAsync(int id, CancellationToken ct)
    {
        var dispatch = await dbContext.Set<Dispatch>()
            .Include(d => d.Project)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (dispatch is null)
        {
            return null;
        }

        var dispatchView = (await ToDispatchViewsAsync([dispatch], ct)).Single();
        var targets = await dbContext.Set<DispatchTarget>()
            .Include(t => t.Contact)
            .Include(t => t.Responses)
            .Where(t => t.DispatchId == id)
            .OrderBy(t => t.Contact == null ? "" : t.Contact.Name)
            .ToListAsync(ct);

        return new NpsDispatchDetailView(dispatchView, targets.Select(ToTargetView).ToList());
    }

    public Task<Dispatch?> GetDispatchEntityAsync(int id, CancellationToken ct)
        => dbContext.Set<Dispatch>().FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<DispatchTarget?> GetTargetByTokenAsync(Guid token, CancellationToken ct)
        => dbContext.Set<DispatchTarget>()
            .Include(t => t.Dispatch)
            .Include(t => t.Project)
            .Include(t => t.Contact)
            .Include(t => t.Responses)
            .FirstOrDefaultAsync(t => t.Token == token, ct);

    public Task<bool> TargetHasResponseAsync(int targetId, CancellationToken ct)
        => dbContext.Set<SurveyResponse>().AnyAsync(r => r.TargetId == targetId, ct);

    public async Task<NpsPublicSurveyView?> GetPublicSurveyAsync(Guid token, CancellationToken ct)
    {
        var target = await GetTargetByTokenAsync(token, ct);
        if (target is null)
        {
            return null;
        }

        return new NpsPublicSurveyView(
            target.Token,
            target.ProjectId,
            target.Project.Name,
            target.DispatchId,
            target.Dispatch.PeriodStart.ToString("yyyy-MM-dd"),
            target.Dispatch.PeriodEnd.ToString("yyyy-MM-dd"),
            FormatFormFormat(target.Dispatch.Format),
            FormatLanguage(target.Dispatch.Language),
            target.Dispatch.ExpiresAt.ToString("O"),
            target.Dispatch.IsExpired(DateTimeOffset.UtcNow),
            // B3/D1: link compartilhado nunca está "já respondido" — ele existe
            // justamente para receber várias respostas.
            !target.IsGeneric && target.Responses.Count != 0);
    }

    public async Task<IReadOnlyList<NpsResponseView>> ListResponsesAsync(int? dispatchId, NpsFilter filter, CancellationToken ct)
    {
        IQueryable<SurveyResponse> query = dbContext.Set<SurveyResponse>()
            .Include(r => r.Project)
            .Include(r => r.Contact);

        if (dispatchId.HasValue)
        {
            query = query.Where(r => r.DispatchId == dispatchId.Value);
        }

        query = ApplyResponseFilters(query, filter);
        query = ApplyProjectResponseFilters(query, filter);

        var responses = await query
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(ct);

        return responses.Select(ToResponseView).ToList();
    }

    public void AddResponse(SurveyResponse response) => dbContext.Set<SurveyResponse>().Add(response);

    public bool IsDuplicateResponseException(Exception exception)
    {
        return exception is DbUpdateException
        {
            InnerException: PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_nps_survey_responses_target_id"
            }
        };
    }

    private async Task<IReadOnlyList<NpsDispatchView>> ToDispatchViewsAsync(IReadOnlyList<Dispatch> dispatches, CancellationToken ct)
    {
        var dispatchIds = dispatches.Select(d => d.Id).ToList();
        var targets = await dbContext.Set<DispatchTarget>()
            .Where(t => dispatchIds.Contains(t.DispatchId))
            .GroupBy(t => t.DispatchId)
            .Select(g => new { DispatchId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DispatchId, x => x.Count, ct);

        var responses = await dbContext.Set<SurveyResponse>()
            .Where(r => dispatchIds.Contains(r.DispatchId))
            .GroupBy(r => r.DispatchId)
            .Select(g => new { DispatchId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DispatchId, x => x.Count, ct);

        return dispatches.Select(d => new NpsDispatchView(
            d.Id,
            d.ProjectId,
            d.Project.Name,
            d.PeriodStart.ToString("yyyy-MM-dd"),
            d.PeriodEnd.ToString("yyyy-MM-dd"),
            FormatFormFormat(d.Format),
            FormatLanguage(d.Language),
            FormatDispatchStatus(d.Status),
            d.CreatedBy,
            d.CreatedAt.ToString("O"),
            d.ClosedAt?.ToString("O"),
            d.ExpiresAt.ToString("O"),
            d.IsExpired(DateTimeOffset.UtcNow),
            targets.GetValueOrDefault(d.Id),
            responses.GetValueOrDefault(d.Id))).ToList();
    }

    private async Task<int> CountOverdueProjectsAsync(IReadOnlyCollection<int> projectIds, CancellationToken ct)
        => (await GetOverdueProjectIdsAsync(projectIds, ct)).Count;

    private async Task<HashSet<int>> GetOverdueProjectIdsAsync(IReadOnlyCollection<int> projectIds, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoff = now.AddDays(-90);

        // Aberto não basta: o link vale 20 dias e nada o fecha sozinho. Disparo
        // vencido não é coleta viva — ninguém consegue mais responder por ele,
        // que é a definição de vencido.
        var activeDispatchProjectIds = await dbContext.Set<Dispatch>()
            .Where(d => projectIds.Contains(d.ProjectId)
                && d.Status == NpsDispatchStatus.Open
                && d.ExpiresAt > now)
            .Select(d => d.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        var recentResponseProjectIds = await dbContext.Set<SurveyResponse>()
            .Where(r => projectIds.Contains(r.ProjectId) && r.SubmittedAt >= cutoff)
            .Select(r => r.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        // F6: projeto com coleta dispensada não está vencido — ninguém está
        // esperando resposta dele. É o critério de aceite do requisito.
        var dismissedProjectIds = await dbContext.Set<CollectionWaiver>()
            .Where(w => projectIds.Contains(w.ProjectId) && w.ReactivatedAt == null)
            .Select(w => w.ProjectId)
            .ToListAsync(ct);

        return projectIds
            .Where(id => !activeDispatchProjectIds.Contains(id)
                && !recentResponseProjectIds.Contains(id)
                && !dismissedProjectIds.Contains(id))
            .ToHashSet();
    }

    private static IQueryable<Project> ApplyProjectFilters(IQueryable<Project> query, NpsFilter filter)
    {
        if (filter.ProjectId.HasValue)
        {
            query = query.Where(p => p.Id == filter.ProjectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Dc))
        {
            query = query.Where(p => p.Dc == ParseDc(filter.Dc));
        }

        if (!string.IsNullOrWhiteSpace(filter.DeliveryManager))
        {
            var deliveryManager = filter.DeliveryManager.Trim().ToLower();
            query = query.Where(p => p.DeliveryManager != null && p.DeliveryManager.ToLower().Contains(deliveryManager));
        }

        if (!string.IsNullOrWhiteSpace(filter.ProjectType))
        {
            query = query.Where(p => p.Type == ParseProjectType(filter.ProjectType));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.Client != null && p.Client.ToLower().Contains(term)));
        }

        return query;
    }

    private static IQueryable<SurveyResponse> ApplyResponseFilters(IQueryable<SurveyResponse> query, NpsFilter filter)
    {
        if (filter.From.HasValue)
        {
            var from = filter.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(r => r.SubmittedAt >= new DateTimeOffset(from));
        }

        if (filter.To.HasValue)
        {
            var to = filter.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(r => r.SubmittedAt <= new DateTimeOffset(to));
        }

        if (filter.Classification.HasValue)
        {
            query = query.Where(r => r.Classification == filter.Classification.Value);
        }

        return query;
    }

    private static IQueryable<SurveyResponse> ApplyProjectResponseFilters(IQueryable<SurveyResponse> query, NpsFilter filter)
    {
        if (filter.ProjectId.HasValue)
        {
            query = query.Where(r => r.ProjectId == filter.ProjectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Dc))
        {
            query = query.Where(r => r.Project.Dc == ParseDc(filter.Dc));
        }

        if (!string.IsNullOrWhiteSpace(filter.DeliveryManager))
        {
            var deliveryManager = filter.DeliveryManager.Trim().ToLower();
            query = query.Where(r => r.Project.DeliveryManager != null && r.Project.DeliveryManager.ToLower().Contains(deliveryManager));
        }

        if (!string.IsNullOrWhiteSpace(filter.ProjectType))
        {
            query = query.Where(r => r.Project.Type == ParseProjectType(filter.ProjectType));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(r =>
                r.Project.Name.ToLower().Contains(term) ||
                (r.Project.Client != null && r.Project.Client.ToLower().Contains(term)) ||
                (r.Comment != null && r.Comment.ToLower().Contains(term)));
        }

        return query;
    }

    private static NpsContactView ToContactView(Contact contact)
        => new(
            contact.Id,
            contact.ProjectId,
            contact.Name,
            contact.Email,
            contact.Role,
            contact.IsArchived,
            contact.CreatedAt.ToString("O"),
            contact.ArchivedAt?.ToString("O"));

    private static NpsProjectView ToProjectView(
        Project project,
        int contactsCount,
        int activeDispatches,
        int linkTargetsCount,
        int answeredLinkTargetsCount,
        int responsesCount,
        SurveyResponse? lastResponse,
        decimal? lastNps,
        bool isOverdue,
        CollectionWaiver? waiver,
        DateTimeOffset? activeDispatchExpiresAt)
        => new(
            project.Id,
            project.Name,
            project.Client,
            FormatDc(project.Dc),
            project.DeliveryManager,
            contactsCount,
            activeDispatches,
            linkTargetsCount,
            answeredLinkTargetsCount,
            responsesCount,
            lastResponse?.SubmittedAt.ToString("O"),
            lastNps,
            isOverdue,
            waiver is not null,
            waiver?.Reason,
            activeDispatchExpiresAt?.ToString("O"));

    private static NpsDispatchTargetView ToTargetView(DispatchTarget target)
        => new(
            target.Id,
            target.DispatchId,
            target.ContactId,
            target.Contact?.Name,
            target.Contact?.Email,
            target.Token,
            target.IsGeneric,
            target.Responses.Count);

    private static NpsResponseView ToResponseView(SurveyResponse response)
        => new(
            response.Id,
            response.ProjectId,
            response.Project.Name,
            response.DispatchId,
            response.TargetId,
            response.ContactId,
            response.Contact?.Name,
            response.Contact?.Email,
            response.Score,
            FormatClassification(response.Classification),
            response.Scope,
            response.Schedule,
            response.Quality,
            response.Communication,
            response.Tags,
            response.Comment,
            response.RespondentName,
            response.RespondentEmail,
            response.SubmittedAt.ToString("O"));

    private static string FormatDc(DeliveryCenter dc) => dc switch
    {
        DeliveryCenter.Dc1 => "DC1",
        DeliveryCenter.Dc2 => "DC2",
        DeliveryCenter.Dc3 => "DC3",
        DeliveryCenter.Dc4 => "DC4",
        DeliveryCenter.Dc5 => "DC5",
        _ => "DC6"
    };

    private static DeliveryCenter ParseDc(string dc) => dc.Trim().ToUpperInvariant() switch
    {
        "DC1" => DeliveryCenter.Dc1,
        "DC2" => DeliveryCenter.Dc2,
        "DC3" => DeliveryCenter.Dc3,
        "DC4" => DeliveryCenter.Dc4,
        "DC5" => DeliveryCenter.Dc5,
        "DC6" => DeliveryCenter.Dc6,
        _ => DeliveryCenter.Dc1
    };

    private static ProjectType ParseProjectType(string projectType) => projectType.Trim().ToLowerInvariant() switch
    {
        "squad" => ProjectType.Squad,
        "escopo fechado" or "fixedscope" or "fixed scope" => ProjectType.FixedScope,
        "alocação" or "alocacao" or "staffing" => ProjectType.Staffing,
        _ => ProjectType.Squad
    };

    private static string FormatFormFormat(NpsFormFormat format) => format switch
    {
        NpsFormFormat.Complete => "Completo",
        _ => "Simplificado"
    };

    private static string FormatLanguage(NpsLanguage language) => language switch
    {
        NpsLanguage.English => "Inglês",
        NpsLanguage.Spanish => "Espanhol",
        _ => "Português"
    };

    private static string FormatDispatchStatus(NpsDispatchStatus status) => status switch
    {
        NpsDispatchStatus.Closed => "Fechado",
        _ => "Aberto"
    };

    private static string FormatClassification(NpsClassification classification) => classification switch
    {
        NpsClassification.Promoter => "Promotor",
        NpsClassification.Passive => "Neutro",
        _ => "Detrator"
    };
}
