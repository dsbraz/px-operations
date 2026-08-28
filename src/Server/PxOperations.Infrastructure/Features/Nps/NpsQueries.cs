using Microsoft.EntityFrameworkCore;
using PxOperations.Application.Features.Nps;
using PxOperations.Domain.Exceptions;
using PxOperations.Domain.Nps;
using PxOperations.Domain.Nps.Calculation;
using PxOperations.Domain.Projects;
using PxOperations.Infrastructure.Persistence;

namespace PxOperations.Infrastructure.Features.Nps;

public sealed class NpsQueries(AppDbContext dbContext) : INpsQueries
{
    public async Task<NpsDashboardView> GetDashboardAsync(
        NpsFilter filter,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var results = await ListProjectResultsAsync(filter, ct);
        var projectIds = results.Select(result => result.Id).ToArray();
        var responses = await ApplyResponsePeriodFilters(dbContext.NpsSurveyResponses.AsNoTracking(), filter)
            .Where(response => projectIds.Contains(response.ProjectId))
            .ToListAsync(ct);
        var metrics = NpsCalculator.Calculate(responses.Select(response => response.Score));
        var counts = responses.GroupBy(response => response.Classification)
            .ToDictionary(group => group.Key, group => group.Count());
        var snapshots = await LoadSnapshotsAsync(
            filter with { Statuses = [], Formats = [], Classifications = [], From = null, To = null },
            now,
            ct);
        var resultIds = projectIds.ToHashSet();

        return new NpsDashboardView(
            metrics.OfficialScore,
            responses.Count,
            metrics.AverageScore,
            snapshots.Count(snapshot => resultIds.Contains(snapshot.Project.Id) && snapshot.IsOverdue(now)),
            new NpsScaleView(NpsScale.MinimumScore, NpsScale.MaximumScore),
            [
                Distribution(NpsClassification.Detractor, counts, metrics.DetractorPercentage),
                Distribution(NpsClassification.Passive, counts, metrics.PassivePercentage),
                Distribution(NpsClassification.Promoter, counts, metrics.PromoterPercentage)
            ],
            await GetFilterOptionsAsync(ct));
    }

    public async Task<NpsFilterOptionsView> GetFilterOptionsAsync(CancellationToken ct)
    {
        var projects = await dbContext.Projects.AsNoTracking().ToListAsync(ct);

        return new NpsFilterOptionsView(
            Options(projects.Select(project => project.Client)),
            Options(projects.Select(project => Dc(project.Dc))),
            Options(projects.Select(project => ProjectTypeCode(project.Type)), projects.Select(project => ProjectTypeLabel(project.Type))),
            Options(projects.Select(project => project.DeliveryManager)),
            new[]
            {
                new NpsOptionView("responded", "Respondido"),
                new NpsOptionView("link_generated", "Link gerado"),
                new NpsOptionView("pending", "Pendente")
            },
            new[]
            {
                new NpsOptionView("complete", "Completo"),
                new NpsOptionView("simplified", "Simplificado")
            },
            new[]
            {
                new NpsOptionView("detractor", "Detrator"),
                new NpsOptionView("passive", "Neutro"),
                new NpsOptionView("promoter", "Promotor")
            });
    }

    public async Task<IReadOnlyList<NpsProjectResultView>> ListProjectResultsAsync(
        NpsFilter filter,
        CancellationToken ct)
    {
        var projectQuery = ApplyProjectFilters(dbContext.Projects.AsNoTracking(), filter);
        if (!filter.IncludeWaived)
        {
            projectQuery = projectQuery.Where(project => !dbContext.NpsCollections.Any(collection =>
                collection.ProjectId == project.Id && collection.WaivedAt != null));
        }
        var allResponseProjectIds = dbContext.NpsSurveyResponses.AsNoTracking()
            .Select(response => response.ProjectId);
        var openDispatchProjectIds = dbContext.NpsCollections.AsNoTracking()
            .Where(collection => collection.Dispatches.Any(dispatch => dispatch.Status == NpsDispatchStatus.Open))
            .Select(collection => collection.ProjectId);

        projectQuery = ApplyProjectResultStatusFilters(
            projectQuery,
            filter.Statuses,
            allResponseProjectIds,
            openDispatchProjectIds);

        var periodResponses = ApplyResponsePeriodFilters(dbContext.NpsSurveyResponses.AsNoTracking(), filter);
        if (filter.From.HasValue || filter.To.HasValue)
        {
            var periodProjectIds = periodResponses.Select(response => response.ProjectId);
            projectQuery = projectQuery.Where(project => periodProjectIds.Contains(project.Id));
        }

        var projects = await projectQuery.OrderBy(project => project.Name).ToListAsync(ct);
        var projectIds = projects.Select(project => project.Id).ToArray();
        var responses = await periodResponses
            .Where(response => projectIds.Contains(response.ProjectId))
            .OrderByDescending(response => response.SubmittedAt)
            .ToListAsync(ct);
        var openProjectIds = (await openDispatchProjectIds
            .Where(projectId => projectIds.Contains(projectId))
            .ToListAsync(ct))
            .ToHashSet();
        var respondedProjectIds = (await allResponseProjectIds
            .Where(projectId => projectIds.Contains(projectId))
            .Distinct()
            .ToListAsync(ct))
            .ToHashSet();
        var byProject = responses.GroupBy(response => response.ProjectId)
            .ToDictionary(group => group.Key, group => group.ToArray());

        return projects.Select(project =>
        {
            var projectResponses = byProject.GetValueOrDefault(project.Id) ?? [];
            var metrics = NpsCalculator.Calculate(projectResponses.Select(response => response.Score));
            var counts = projectResponses.GroupBy(response => response.Classification)
                .ToDictionary(group => group.Key, group => group.Count());
            var status = NpsProjectResultPolicy.DetermineStatus(
                respondedProjectIds.Contains(project.Id),
                openProjectIds.Contains(project.Id));

            return new NpsProjectResultView(
                project.Id,
                project.Name,
                project.Client,
                Dc(project.Dc),
                project.DeliveryManager,
                projectResponses.Length,
                metrics.OfficialScore,
                [
                    Distribution(NpsClassification.Detractor, counts, metrics.DetractorPercentage),
                    Distribution(NpsClassification.Passive, counts, metrics.PassivePercentage),
                    Distribution(NpsClassification.Promoter, counts, metrics.PromoterPercentage)
                ],
                [
                    new NpsFormatCountView("complete", "Completo", projectResponses.Count(response => response.Format == NpsFormFormat.Complete)),
                    new NpsFormatCountView("simplified", "Simplificado", projectResponses.Count(response => response.Format == NpsFormFormat.Simplified))
                ],
                projectResponses.FirstOrDefault()?.SubmittedAt,
                ProjectResultStatus(status));
        }).ToArray();
    }

    public async Task<IReadOnlyList<NpsResponseView>> ListResponsesAsync(
        NpsFilter filter,
        CancellationToken ct)
    {
        var responses = await ApplyResponseFilters(dbContext.NpsSurveyResponses.AsNoTracking(), filter)
            .OrderByDescending(response => response.SubmittedAt)
            .ThenByDescending(response => response.Id)
            .ToListAsync(ct);
        return await ToResponseViewsAsync(responses, ct);
    }

    public async Task<IReadOnlyList<NpsProjectView>> ListProjectsAsync(
        NpsFilter filter,
        DateTimeOffset now,
        CancellationToken ct)
        => (await LoadSnapshotsAsync(filter, now, ct))
            .Select(snapshot => ToProjectView(snapshot, now))
            .OrderBy(project => project.Name)
            .ToArray();

    public async Task<NpsProjectDetailView?> GetProjectAsync(int projectId, DateTimeOffset now, CancellationToken ct)
    {
        var filter = NpsFilter.Empty with { ProjectId = projectId, IncludeWaived = true };
        var snapshot = (await LoadSnapshotsAsync(filter, now, ct)).SingleOrDefault();
        if (snapshot is null)
        {
            return null;
        }

        var metrics = NpsCalculator.Calculate(snapshot.Responses.Select(response => response.Score));
        var responseViews = await ToResponseViewsAsync(snapshot.Responses.OrderByDescending(response => response.SubmittedAt).Take(20), ct);
        var project = ToProjectView(snapshot, now);

        return new NpsProjectDetailView(
            project,
            metrics.OfficialScore,
            metrics.AverageScore,
            snapshot.Responses.Count,
            snapshot.Responses.Count(response => response.Classification == NpsClassification.Promoter),
            project.ActiveLinks,
            responseViews);
    }

    public async Task<IReadOnlyList<NpsResponseView>> ListProjectResponsesAsync(
        int projectId,
        NpsFilter filter,
        CancellationToken ct)
        => await ListResponsesAsync(filter with { ProjectId = projectId }, ct);

    public async Task<IReadOnlyList<NpsContactView>> ListContactsAsync(
        int projectId,
        bool includeArchived,
        CancellationToken ct)
    {
        var query = dbContext.NpsContacts.AsNoTracking().Where(contact => contact.ProjectId == projectId);
        if (!includeArchived)
        {
            query = query.Where(contact => !contact.IsArchived);
        }

        return await query.OrderBy(contact => contact.Name).Select(contact => new NpsContactView(
            contact.Id,
            contact.ProjectId,
            contact.Name,
            contact.Email,
            contact.Role,
            contact.IsArchived,
            contact.CreatedAt,
            contact.ArchivedAt)).ToListAsync(ct);
    }

    public Task<NpsContactView?> GetContactAsync(int id, CancellationToken ct)
        => dbContext.NpsContacts.AsNoTracking()
            .Where(contact => contact.Id == id)
            .Select(contact => new NpsContactView(
                contact.Id,
                contact.ProjectId,
                contact.Name,
                contact.Email,
                contact.Role,
                contact.IsArchived,
                contact.CreatedAt,
                contact.ArchivedAt))
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<NpsDispatchView>> ListDispatchesAsync(
        int projectId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var collectionId = await dbContext.NpsCollections.AsNoTracking()
            .Where(collection => collection.ProjectId == projectId)
            .Select(collection => (int?)collection.Id)
            .SingleOrDefaultAsync(ct);
        if (!collectionId.HasValue)
        {
            return [];
        }

        var dispatches = await dbContext.NpsDispatches.AsNoTracking()
            .Where(dispatch => dispatch.CollectionId == collectionId.Value)
            .OrderByDescending(dispatch => dispatch.CreatedAt)
            .ToListAsync(ct);
        var project = await dbContext.Projects.AsNoTracking().SingleAsync(item => item.Id == projectId, ct);
        return await ToDispatchViewsAsync(dispatches, project, now, ct);
    }

    public async Task<NpsDispatchDetailView?> GetDispatchAsync(int id, DateTimeOffset now, CancellationToken ct)
    {
        var dispatch = await dbContext.NpsDispatches.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, ct);
        if (dispatch is null)
        {
            return null;
        }

        var collection = await dbContext.NpsCollections.AsNoTracking().SingleAsync(item => item.Id == dispatch.CollectionId, ct);
        var project = await dbContext.Projects.AsNoTracking().SingleAsync(item => item.Id == collection.ProjectId, ct);
        var dispatchView = (await ToDispatchViewsAsync([dispatch], project, now, ct)).Single();
        var targets = await dbContext.NpsDispatchTargets.AsNoTracking()
            .Where(target => target.DispatchId == id)
            .OrderBy(target => target.ContactId)
            .ToListAsync(ct);
        var contactIds = targets.Where(target => target.ContactId.HasValue).Select(target => target.ContactId!.Value).ToArray();
        var contacts = await dbContext.NpsContacts.AsNoTracking()
            .Where(contact => contactIds.Contains(contact.Id))
            .ToDictionaryAsync(contact => contact.Id, ct);
        var counts = await dbContext.NpsSurveyResponses.AsNoTracking()
            .Where(response => response.DispatchId == id)
            .GroupBy(response => response.TargetId)
            .Select(group => new { TargetId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.TargetId, item => item.Count, ct);

        return new NpsDispatchDetailView(
            dispatchView,
            targets.Select(target =>
            {
                var contact = target.ContactId.HasValue ? contacts.GetValueOrDefault(target.ContactId.Value) : null;
                return new NpsDispatchTargetView(
                    target.Id,
                    target.DispatchId,
                    target.ContactId,
                    contact?.Name,
                    contact?.Email,
                    target.Token,
                    target.IsGeneric,
                    counts.GetValueOrDefault(target.Id));
            }).ToArray());
    }

    public async Task<IReadOnlyList<NpsResponseView>> ListDispatchResponsesAsync(int dispatchId, CancellationToken ct)
    {
        var responses = await dbContext.NpsSurveyResponses.AsNoTracking()
            .Where(response => response.DispatchId == dispatchId)
            .OrderByDescending(response => response.SubmittedAt)
            .ToListAsync(ct);
        return await ToResponseViewsAsync(responses, ct);
    }

    public async Task<NpsPublicSurveyView?> GetPublicSurveyAsync(Guid token, DateTimeOffset now, CancellationToken ct)
    {
        var target = await dbContext.NpsDispatchTargets.AsNoTracking().SingleOrDefaultAsync(item => item.Token == token, ct);
        if (target is null)
        {
            return null;
        }

        var dispatch = await dbContext.NpsDispatches.AsNoTracking().SingleAsync(item => item.Id == target.DispatchId, ct);
        var collection = await dbContext.NpsCollections.AsNoTracking().SingleAsync(item => item.Id == dispatch.CollectionId, ct);
        var project = await dbContext.Projects.AsNoTracking().SingleAsync(item => item.Id == collection.ProjectId, ct);
        var answered = target.ContactId.HasValue && await dbContext.NpsSurveyResponses.AsNoTracking()
            .AnyAsync(response => response.TargetId == target.Id, ct);
        var availability = PublicAvailability(collection, dispatch, answered, now);

        return new NpsPublicSurveyView(
            token,
            project.Id,
            project.Name,
            project.Client,
            dispatch.Id,
            NpsCodes.Format(dispatch.Format),
            NpsCodes.Language(dispatch.Language),
            dispatch.ExpiresAt,
            availability,
            target.IsGeneric,
            new NpsScaleView(NpsScale.MinimumScore, NpsScale.MaximumScore),
            dispatch.Format == NpsFormFormat.Complete ? Aspects(dispatch.Language) : []);
    }

    public async Task<NpsResponseView?> GetResponseAsync(int id, CancellationToken ct)
    {
        var response = await dbContext.NpsSurveyResponses.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, ct);
        return response is null ? null : (await ToResponseViewsAsync([response], ct)).Single();
    }

    private async Task<IReadOnlyList<ProjectSnapshot>> LoadSnapshotsAsync(
        NpsFilter filter,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var projects = await ApplyProjectFilters(dbContext.Projects.AsNoTracking(), filter).ToListAsync(ct);
        var projectIds = projects.Select(project => project.Id).ToArray();
        var collections = await dbContext.NpsCollections.AsNoTracking()
            .Include(collection => collection.Dispatches)
            .ThenInclude(dispatch => dispatch.Targets)
            .Where(collection => projectIds.Contains(collection.ProjectId))
            .ToDictionaryAsync(collection => collection.ProjectId, ct);
        var responses = await dbContext.NpsSurveyResponses.AsNoTracking()
            .Where(response => projectIds.Contains(response.ProjectId))
            .ToListAsync(ct);
        var byProject = responses.GroupBy(response => response.ProjectId).ToDictionary(group => group.Key, group => group.ToList());

        var snapshots = projects.Select(project => new ProjectSnapshot(
            project,
            collections.GetValueOrDefault(project.Id),
            byProject.GetValueOrDefault(project.Id) ?? [])).ToList();

        if (!filter.IncludeWaived)
        {
            snapshots = snapshots.Where(snapshot => !snapshot.IsWaived).ToList();
        }

        return snapshots;
    }

    private static IQueryable<Project> ApplyProjectFilters(IQueryable<Project> query, NpsFilter filter)
    {
        if (filter.ProjectId.HasValue)
        {
            query = query.Where(project => project.Id == filter.ProjectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{filter.Search.Trim()}%";
            query = query.Where(project =>
                EF.Functions.ILike(project.Name, pattern) ||
                (project.Client != null && EF.Functions.ILike(project.Client, pattern)));
        }

        if (filter.Clients.Count != 0)
        {
            query = query.Where(project => project.Client != null && filter.Clients.Contains(project.Client));
        }

        if (filter.Dcs.Count != 0)
        {
            var values = filter.Dcs.Select(ParseDc).ToArray();
            query = query.Where(project => values.Contains(project.Dc));
        }

        if (filter.ProjectTypes.Count != 0)
        {
            var values = filter.ProjectTypes.Select(ParseProjectType).ToArray();
            query = query.Where(project => values.Contains(project.Type));
        }

        if (filter.DeliveryManagers.Count != 0)
        {
            query = query.Where(project => project.DeliveryManager != null && filter.DeliveryManagers.Contains(project.DeliveryManager));
        }

        return query;
    }

    private IQueryable<SurveyResponse> ApplyResponseFilters(
        IQueryable<SurveyResponse> query,
        NpsFilter filter)
    {
        var projectQuery = ApplyProjectFilters(
            dbContext.Projects.AsNoTracking(),
            filter with { Search = null });
        query = query.Where(response => projectQuery.Select(project => project.Id).Contains(response.ProjectId));

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{filter.Search.Trim()}%";
            query = query.Where(response =>
                dbContext.Projects.Any(project =>
                    project.Id == response.ProjectId && EF.Functions.ILike(project.Name, pattern)) ||
                (response.RespondentName != null && EF.Functions.ILike(response.RespondentName, pattern)) ||
                (response.RespondentEmail != null && EF.Functions.ILike(response.RespondentEmail, pattern)) ||
                (response.Comment != null && EF.Functions.ILike(response.Comment, pattern)) ||
                (response.ContactId.HasValue && dbContext.NpsContacts.Any(contact =>
                    contact.Id == response.ContactId.Value &&
                    (EF.Functions.ILike(contact.Name, pattern) || EF.Functions.ILike(contact.Email, pattern)))));
        }

        query = ApplyResponsePeriodFilters(query, filter);

        if (filter.Formats.Count != 0)
        {
            var formats = filter.Formats.Select(NpsCodes.ParseFormat).ToArray();
            query = query.Where(response => formats.Contains(response.Format));
        }

        if (filter.Classifications.Count != 0)
        {
            var classifications = filter.Classifications.Select(ParseClassification).ToArray();
            query = query.Where(response => classifications.Contains(response.Classification));
        }

        return query;
    }

    private static IQueryable<SurveyResponse> ApplyResponsePeriodFilters(
        IQueryable<SurveyResponse> query,
        NpsFilter filter)
    {
        if (filter.From.HasValue)
        {
            var from = new DateTimeOffset(filter.From.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(response => response.SubmittedAt >= from);
        }

        if (filter.To.HasValue)
        {
            var until = new DateTimeOffset(filter.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(response => response.SubmittedAt < until);
        }

        return query;
    }

    private static IQueryable<Project> ApplyProjectResultStatusFilters(
        IQueryable<Project> query,
        IReadOnlyList<string> statuses,
        IQueryable<int> responseProjectIds,
        IQueryable<int> openDispatchProjectIds)
    {
        if (statuses.Count == 0)
        {
            return query;
        }

        var responded = statuses.Contains("responded", StringComparer.OrdinalIgnoreCase);
        var linkGenerated = statuses.Contains("link_generated", StringComparer.OrdinalIgnoreCase);
        var pending = statuses.Contains("pending", StringComparer.OrdinalIgnoreCase);
        return query.Where(project =>
            (responded && responseProjectIds.Contains(project.Id)) ||
            (linkGenerated && !responseProjectIds.Contains(project.Id) && openDispatchProjectIds.Contains(project.Id)) ||
            (pending && !responseProjectIds.Contains(project.Id) && !openDispatchProjectIds.Contains(project.Id)));
    }

    private NpsProjectView ToProjectView(ProjectSnapshot snapshot, DateTimeOffset now)
    {
        var stage = snapshot.Stage(now);
        var openStates = snapshot.OpenStates;
        var domainAction = NpsCollectionPolicy.DeterminePrimaryAction(
            stage,
            openStates,
            snapshot.MostRecentFormat,
            now);
        var links = snapshot.OpenDispatches
            .Select(dispatch => ToLinkView(dispatch, now))
            .Where(link => link is not null)
            .Cast<NpsLinkView>()
            .OrderBy(link => link.Format)
            .ToArray();

        return new NpsProjectView(
            snapshot.Project.Id,
            snapshot.Project.Name,
            snapshot.Project.Client,
            Dc(snapshot.Project.Dc),
            snapshot.Project.DeliveryManager,
            ProjectTypeLabel(snapshot.Project.Type),
            snapshot.Responses.Count,
            Stage(stage),
            Temporal(snapshot, stage, domainAction, now),
            snapshot.Collection?.IsWaived == true
                ? new NpsWaiverView(snapshot.Collection.WaiverReason!, snapshot.Collection.WaivedAt!.Value)
                : null,
            links,
            PrimaryAction(domainAction, stage, links),
            snapshot.IsOverdue(now),
            snapshot.LastDispatchClosedAt);
    }

    private static NpsLinkView? ToLinkView(Dispatch dispatch, DateTimeOffset now)
    {
        var target = dispatch.Targets.FirstOrDefault(item => item.IsGeneric);
        if (target is null)
        {
            return null;
        }

        var expired = NpsCollectionPolicy.IsExpired(dispatch.ExpiresAt, now);
        var warning = NpsCollectionPolicy.IsExpiringSoon(dispatch.ExpiresAt, now);
        return new NpsLinkView(
            dispatch.Id,
            target.Token,
            NpsCodes.Format(dispatch.Format),
            NpsCodes.FormatLabel(dispatch.Format),
            dispatch.ExpiresAt,
            expired ? "expired" : "open",
            expired ? "Expirado" : "Aberto",
            expired ? "critical" : warning ? "warning" : "neutral");
    }

    private static NpsBadgeView Stage(NpsCollectionStage stage) => stage switch
    {
        NpsCollectionStage.NoLink => new("no_link", "Sem link", "neutral"),
        NpsCollectionStage.AwaitingResponse => new("awaiting_response", "Aguardando resposta", "info"),
        NpsCollectionStage.Recollection => new("recollection", "Recoleta", "warning"),
        NpsCollectionStage.Current => new("current", "Em dia", "positive"),
        _ => new("waived", "Dispensado", "neutral")
    };

    private static NpsBadgeView ProjectResultStatus(NpsProjectResultStatus status) => status switch
    {
        NpsProjectResultStatus.Responded => new("responded", "Respondido", "positive"),
        NpsProjectResultStatus.LinkGenerated => new("link_generated", "Link gerado", "info"),
        _ => new("pending", "Pendente", "neutral")
    };

    private static NpsTemporalView Temporal(
        ProjectSnapshot snapshot,
        NpsCollectionStage stage,
        NpsPrimaryAction? action,
        DateTimeOffset now)
    {
        if (stage == NpsCollectionStage.Waived)
        {
            var at = snapshot.Collection!.WaivedAt!.Value;
            return new NpsTemporalView($"Dispensado em {at:dd/MM/yyyy}", "neutral", at);
        }

        if (stage == NpsCollectionStage.AwaitingResponse && action?.DispatchId is int dispatchId)
        {
            var dispatch = snapshot.OpenDispatches.Single(item => item.Id == dispatchId);
            if (NpsCollectionPolicy.IsExpired(dispatch.ExpiresAt, now))
            {
                return new NpsTemporalView($"Link expirado há {Days(now - dispatch.ExpiresAt)}d", "critical", dispatch.ExpiresAt);
            }

            var tone = NpsCollectionPolicy.IsExpiringSoon(dispatch.ExpiresAt, now) ? "warning" : "neutral";
            return new NpsTemporalView($"Expira em {Math.Max(1, (int)Math.Ceiling((dispatch.ExpiresAt - now).TotalDays))}d", tone, dispatch.ExpiresAt);
        }

        if (stage == NpsCollectionStage.NoLink)
        {
            return snapshot.LastDispatchClosedAt is { } closedAt
                ? new NpsTemporalView($"Sem link há {Days(now - closedAt)}d", "neutral", closedAt)
                : new NpsTemporalView("Nunca coletado", "neutral", null);
        }

        var lastResponseAt = snapshot.LastResponseAt;
        return new NpsTemporalView($"Última resposta há {Days(now - lastResponseAt!.Value)}d", "neutral", lastResponseAt);
    }

    private static NpsPrimaryActionView? PrimaryAction(
        NpsPrimaryAction? action,
        NpsCollectionStage stage,
        IReadOnlyList<NpsLinkView> links)
    {
        if (action is null)
        {
            return null;
        }

        var code = action.Kind switch
        {
            NpsPrimaryActionKind.Reactivate => "reactivate",
            NpsPrimaryActionKind.CopyLink => "copy_link",
            _ => "generate_link"
        };
        var label = action.Kind switch
        {
            NpsPrimaryActionKind.Reactivate => "Reativar",
            NpsPrimaryActionKind.CopyLink => "Copiar link",
            _ when stage == NpsCollectionStage.AwaitingResponse => "Gerar novo link",
            _ => "Gerar link"
        };
        var link = action.DispatchId.HasValue ? links.FirstOrDefault(item => item.DispatchId == action.DispatchId.Value) : null;
        return new NpsPrimaryActionView(
            code,
            label,
            action.Format.HasValue ? NpsCodes.Format(action.Format.Value) : null,
            action.DispatchId,
            link?.Token);
    }

    private async Task<IReadOnlyList<NpsDispatchView>> ToDispatchViewsAsync(
        IReadOnlyList<Dispatch> dispatches,
        Project project,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var ids = dispatches.Select(dispatch => dispatch.Id).ToArray();
        var targetCounts = await dbContext.NpsDispatchTargets.AsNoTracking()
            .Where(target => ids.Contains(target.DispatchId))
            .GroupBy(target => target.DispatchId)
            .Select(group => new { DispatchId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.DispatchId, item => item.Count, ct);
        var responseCounts = await dbContext.NpsSurveyResponses.AsNoTracking()
            .Where(response => ids.Contains(response.DispatchId))
            .GroupBy(response => response.DispatchId)
            .Select(group => new { DispatchId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.DispatchId, item => item.Count, ct);

        return dispatches.Select(dispatch =>
        {
            var availability = DispatchAvailability(dispatch, now);
            return new NpsDispatchView(
                dispatch.Id,
                project.Id,
                project.Name,
                NpsCodes.Format(dispatch.Format),
                NpsCodes.FormatLabel(dispatch.Format),
                NpsCodes.Language(dispatch.Language),
                NpsCodes.LanguageLabel(dispatch.Language),
                dispatch.IsOpen ? "open" : "closed",
                dispatch.CreatedAt,
                dispatch.ExpiresAt,
                dispatch.ClosedAt,
                targetCounts.GetValueOrDefault(dispatch.Id),
                responseCounts.GetValueOrDefault(dispatch.Id),
                availability.Code,
                availability.Label,
                availability.Tone);
        }).ToArray();
    }

    private async Task<IReadOnlyList<NpsResponseView>> ToResponseViewsAsync(
        IEnumerable<SurveyResponse> source,
        CancellationToken ct)
    {
        var responses = source.ToArray();
        var projectIds = responses.Select(response => response.ProjectId).Distinct().ToArray();
        var contactIds = responses.Where(response => response.ContactId.HasValue).Select(response => response.ContactId!.Value).Distinct().ToArray();
        var projects = await dbContext.Projects.AsNoTracking().Where(project => projectIds.Contains(project.Id)).ToDictionaryAsync(project => project.Id, ct);
        var contacts = await dbContext.NpsContacts.AsNoTracking().Where(contact => contactIds.Contains(contact.Id)).ToDictionaryAsync(contact => contact.Id, ct);

        return responses.Select(response =>
        {
            var contact = response.ContactId.HasValue ? contacts.GetValueOrDefault(response.ContactId.Value) : null;
            return new NpsResponseView(
                response.Id,
                response.ProjectId,
                projects[response.ProjectId].Name,
                response.DispatchId,
                response.TargetId,
                response.ContactId,
                contact?.Name,
                contact?.Email,
                NpsCodes.Format(response.Format),
                NpsCodes.FormatLabel(response.Format),
                response.Score,
                NpsCodes.Classification(response.Classification),
                NpsCodes.ClassificationLabel(response.Classification),
                response.Quality,
                response.Schedule,
                response.Communication,
                response.BusinessValue,
                response.Comment,
                response.RespondentName,
                response.RespondentEmail,
                response.SubmittedAt);
        }).ToArray();
    }

    private static (string Code, string Label, string Tone) DispatchAvailability(Dispatch dispatch, DateTimeOffset now)
    {
        if (!dispatch.IsOpen)
        {
            return ("closed", "Encerrado", "neutral");
        }

        if (NpsCollectionPolicy.IsExpired(dispatch.ExpiresAt, now))
        {
            return ("expired", "Expirado", "critical");
        }

        return NpsCollectionPolicy.IsExpiringSoon(dispatch.ExpiresAt, now)
            ? ("open", "Aberto", "warning")
            : ("open", "Aberto", "positive");
    }

    private static string PublicAvailability(NpsCollection collection, Dispatch dispatch, bool answered, DateTimeOffset now)
    {
        if (collection.IsWaived)
        {
            return "waived";
        }

        if (!dispatch.IsOpen)
        {
            return "closed";
        }

        if (NpsCollectionPolicy.IsExpired(dispatch.ExpiresAt, now))
        {
            return "expired";
        }

        return answered ? "already_answered" : "open";
    }

    private static IReadOnlyList<NpsAspectView> Aspects(NpsLanguage language)
    {
        var labels = language switch
        {
            NpsLanguage.English => new[] { "Quality", "Schedule", "Communication", "Business value" },
            NpsLanguage.Spanish => new[] { "Calidad", "Plazo", "Comunicación", "Valor para el negocio" },
            _ => new[] { "Qualidade", "Prazo", "Comunicação", "Valor para o negócio" }
        };
        var codes = new[] { "quality", "schedule", "communication", "businessValue" };
        return codes.Select((code, index) => new NpsAspectView(
            code,
            labels[index],
            new NpsScaleView(NpsScale.MinimumAspect, NpsScale.MaximumAspect))).ToArray();
    }

    private static NpsDistributionView Distribution(
        NpsClassification classification,
        IReadOnlyDictionary<NpsClassification, int> counts,
        decimal percentage)
        => new(
            NpsCodes.Classification(classification),
            NpsCodes.ClassificationLabel(classification),
            classification switch
            {
                NpsClassification.Detractor => "critical",
                NpsClassification.Passive => "warning",
                _ => "positive"
            },
            counts.GetValueOrDefault(classification),
            percentage);

    private static IReadOnlyList<NpsOptionView> Options(IEnumerable<string?> values)
        => values.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
            .Select(value => new NpsOptionView(value, value))
            .ToArray();

    private static IReadOnlyList<NpsOptionView> Options(IEnumerable<string> codes, IEnumerable<string> labels)
        => codes.Zip(labels)
            .Distinct()
            .Select(pair => new NpsOptionView(pair.First, pair.Second))
            .OrderBy(option => option.Label, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

    private static int Days(TimeSpan span) => Math.Max(0, (int)Math.Floor(span.TotalDays));
    private static string Dc(DeliveryCenter value) => value.ToString().ToUpperInvariant();
    private static string ProjectTypeCode(ProjectType value) => value switch
    {
        ProjectType.Squad => "squad",
        ProjectType.FixedScope => "fixed_scope",
        _ => "staffing"
    };

    private static string ProjectTypeLabel(ProjectType value) => value switch
    {
        ProjectType.Squad => "Squad",
        ProjectType.FixedScope => "Escopo fechado",
        _ => "Staffing"
    };

    private static DeliveryCenter ParseDc(string value)
        => Enum.TryParse<DeliveryCenter>(value, true, out var parsed)
            ? parsed
            : throw new BusinessRuleValidationException("Invalid delivery center.");

    private static ProjectType ParseProjectType(string value) => value.Trim().ToLowerInvariant() switch
    {
        "squad" => ProjectType.Squad,
        "fixed_scope" => ProjectType.FixedScope,
        "staffing" => ProjectType.Staffing,
        _ => throw new BusinessRuleValidationException("Invalid project type.")
    };

    private static NpsClassification ParseClassification(string value) => value.Trim().ToLowerInvariant() switch
    {
        "detractor" => NpsClassification.Detractor,
        "passive" => NpsClassification.Passive,
        "promoter" => NpsClassification.Promoter,
        _ => throw new BusinessRuleValidationException("Invalid NPS classification.")
    };

    private sealed record ProjectSnapshot(
        Project Project,
        NpsCollection? Collection,
        IReadOnlyList<SurveyResponse> Responses)
    {
        public bool IsWaived => Collection?.IsWaived == true;
        public IReadOnlyList<Dispatch> OpenDispatches => Collection?.Dispatches.Where(dispatch => dispatch.IsOpen).ToArray() ?? [];
        public IReadOnlyList<NpsOpenDispatchState> OpenStates => OpenDispatches.Select(dispatch => new NpsOpenDispatchState(
            dispatch.Id,
            dispatch.Format,
            dispatch.ExpiresAt,
            Responses.Any(response => response.DispatchId == dispatch.Id))).ToArray();
        public DateTimeOffset? LastResponseAt => Responses.Count == 0 ? null : Responses.Max(response => response.SubmittedAt);
        public DateTimeOffset? LastDispatchClosedAt => Collection?.Dispatches.Where(dispatch => dispatch.ClosedAt.HasValue).Max(dispatch => dispatch.ClosedAt);
        public NpsFormFormat? MostRecentFormat => Collection?.Dispatches.OrderByDescending(dispatch => dispatch.CreatedAt).Select(dispatch => (NpsFormFormat?)dispatch.Format).FirstOrDefault();
        public NpsCollectionStage Stage(DateTimeOffset now) => NpsCollectionPolicy.DetermineStage(IsWaived, OpenStates, LastResponseAt, now);
        public bool IsOverdue(DateTimeOffset now) => NpsCollectionPolicy.IsOverdue(IsWaived, OpenDispatches.Count != 0, LastResponseAt, now);
    }
}
