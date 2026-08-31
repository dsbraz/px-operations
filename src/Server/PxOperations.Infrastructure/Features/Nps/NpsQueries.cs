using Microsoft.EntityFrameworkCore;
using PxOperations.Application.Features.Nps;
using PxOperations.Domain.Nps;
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
        // As respostas do período já foram materializadas para montar os
        // resultados por projeto; relê-las aqui, e uma terceira vez para o
        // agregado de aspectos, custava duas varreduras extras da tabela a cada
        // toggle de faceta e a cada mudança de data.
        var (_, responses) = await LoadProjectResultsAsync(filter, ct);
        var metrics = NpsCalculator.Calculate(responses.Select(response => response.Score));
        var counts = responses.GroupBy(response => response.Classification)
            .ToDictionary(group => group.Key, group => group.Count());
        var completeResponses = responses
            .Where(response => response.Format == NpsFormFormat.Complete)
            .ToArray();
        // Os vencidos são lidos com o período zerado de propósito: "está sem
        // coleta há mais de 90 dias" não é um fato da janela escolhida. Recortar
        // os snapshots pelos projetos do resultado desfazia isso, porque quem
        // não respondeu dentro da janela já tinha saído do resultado — e o
        // indicador zerava justamente quando um período era selecionado.
        var snapshots = await LoadSnapshotsAsync(
            filter with { Statuses = [], Formats = [], Classifications = [], From = null, To = null },
            now,
            ct);

        return new NpsDashboardView(
            metrics.OfficialScore,
            responses.Count,
            metrics.AverageScore,
            snapshots.Count(snapshot => snapshot.IsOverdue(now)),
            new NpsScaleView(NpsScale.MinimumScore, NpsScale.MaximumScore),
            [
                NpsViewMappings.Distribution(NpsClassification.Detractor, counts, metrics.DetractorPercentage),
                NpsViewMappings.Distribution(NpsClassification.Passive, counts, metrics.PassivePercentage),
                NpsViewMappings.Distribution(NpsClassification.Promoter, counts, metrics.PromoterPercentage)
            ],
            new NpsAspectSummaryView(
                completeResponses.Length,
                new NpsScaleView(NpsScale.MinimumAspect, NpsScale.MaximumAspect),
                [
                    NpsViewMappings.Aspect("quality", "Qualidade técnica", completeResponses, response => response.Quality),
                    NpsViewMappings.Aspect("schedule", "Prazos acordados", completeResponses, response => response.Schedule),
                    NpsViewMappings.Aspect("communication", "Comunicação", completeResponses, response => response.Communication),
                    NpsViewMappings.Aspect("business_value", "Valor para o negócio", completeResponses, response => response.BusinessValue)
                ]),
            await GetFilterOptionsAsync(ct));
    }

    public async Task<NpsFilterOptionsView> GetFilterOptionsAsync(CancellationToken ct)
    {
        // Só quatro colunas alimentam as listas de opções; materializar a
        // entidade inteira trazia todo o resto da tabela junto.
        var projects = await dbContext.Projects.AsNoTracking()
            .Select(project => new
            {
                project.Client,
                project.Dc,
                project.Type,
                project.DeliveryManager
            })
            .ToListAsync(ct);

        return new NpsFilterOptionsView(
            NpsViewMappings.Options(projects.Select(project => project.Client)),
            NpsViewMappings.Options(projects.Select(project => NpsViewMappings.Dc(project.Dc))),
            NpsViewMappings.Options(projects.Select(project => NpsViewMappings.ProjectTypeCode(project.Type)), projects.Select(project => NpsViewMappings.ProjectTypeLabel(project.Type))),
            NpsViewMappings.Options(projects.Select(project => project.DeliveryManager)),
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
        => (await LoadProjectResultsAsync(filter, ct)).Results;

    private async Task<(IReadOnlyList<NpsProjectResultView> Results, IReadOnlyList<SurveyResponse> Responses)>
        LoadProjectResultsAsync(NpsFilter filter, CancellationToken ct)
    {
        var projectQuery = NpsQueryFilters.ApplyProjectFilters(dbContext.Projects.AsNoTracking(), filter);
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

        projectQuery = NpsQueryFilters.ApplyProjectResultStatusFilters(
            projectQuery,
            filter.Statuses,
            allResponseProjectIds,
            openDispatchProjectIds);

        var periodResponses = NpsQueryFilters.ApplyResponsePeriodFilters(dbContext.NpsSurveyResponses.AsNoTracking(), filter);
        if (filter.From.HasValue || filter.To.HasValue)
        {
            var periodProjectIds = periodResponses.Select(response => response.ProjectId);

            // "Pendente" e "Link gerado" significam projeto sem nenhuma
            // resposta, e o período exige resposta dentro da janela: os dois
            // predicados se anulavam e a lista voltava vazia sem explicação.
            // Um projeto que nunca respondeu não é filtrado por um período de
            // respostas — mas só entra quando o status foi pedido, senão a
            // visão padrão com período encheria de projeto sem coleta.
            if (NpsQueryFilters.WantsProjectsWithoutResponses(filter.Statuses))
            {
                projectQuery = projectQuery.Where(project =>
                    periodProjectIds.Contains(project.Id) ||
                    !allResponseProjectIds.Contains(project.Id));
            }
            else
            {
                projectQuery = projectQuery.Where(project => periodProjectIds.Contains(project.Id));
            }
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

        var results = projects.Select(project =>
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
                NpsViewMappings.Dc(project.Dc),
                project.DeliveryManager,
                projectResponses.Length,
                metrics.OfficialScore,
                [
                    NpsViewMappings.Distribution(NpsClassification.Detractor, counts, metrics.DetractorPercentage),
                    NpsViewMappings.Distribution(NpsClassification.Passive, counts, metrics.PassivePercentage),
                    NpsViewMappings.Distribution(NpsClassification.Promoter, counts, metrics.PromoterPercentage)
                ],
                [
                    new NpsFormatCountView("complete", "Completo", projectResponses.Count(response => response.Format == NpsFormFormat.Complete)),
                    new NpsFormatCountView("simplified", "Simplificado", projectResponses.Count(response => response.Format == NpsFormFormat.Simplified))
                ],
                projectResponses.FirstOrDefault()?.SubmittedAt,
                NpsViewMappings.ProjectResultStatus(status));
        }).ToArray();

        return (results, responses);
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
            .Select(snapshot => NpsViewMappings.ToProjectView(snapshot, now))
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
        // O snapshot carrega só as colunas do painel; o detalhe precisa da
        // resposta inteira, mas de um projeto só e das vinte mais recentes.
        var recent = await dbContext.NpsSurveyResponses.AsNoTracking()
            .Where(response => response.ProjectId == projectId)
            .OrderByDescending(response => response.SubmittedAt)
            .Take(20)
            .ToListAsync(ct);
        var responseViews = await ToResponseViewsAsync(recent, ct);
        var project = NpsViewMappings.ToProjectView(snapshot, now);

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
        var availability = NpsViewMappings.PublicAvailability(collection, dispatch, answered, now);

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
            dispatch.Format == NpsFormFormat.Complete ? NpsViewMappings.Aspects(dispatch.Language) : []);
    }

    public async Task<NpsResponseView?> GetResponseAsync(int id, CancellationToken ct)
    {
        var response = await dbContext.NpsSurveyResponses.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, ct);
        return response is null ? null : (await ToResponseViewsAsync([response], ct)).Single();
    }

    private async Task<IReadOnlyList<NpsProjectSnapshot>> LoadSnapshotsAsync(
        NpsFilter filter,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var projects = await NpsQueryFilters.ApplyProjectFilters(dbContext.Projects.AsNoTracking(), filter).ToListAsync(ct);
        var projectIds = projects.Select(project => project.Id).ToArray();
        var collections = await dbContext.NpsCollections.AsNoTracking()
            .Include(collection => collection.Dispatches)
            .ThenInclude(dispatch => dispatch.Targets)
            .Where(collection => projectIds.Contains(collection.ProjectId))
            .ToDictionaryAsync(collection => collection.ProjectId, ct);
        // Só estas colunas alimentam o snapshot; materializar a entidade
        // inteira trazia comentário, nome, e-mail e os quatro aspectos de cada
        // resposta da carteira a cada recarga da aba Coleta.
        var responses = await dbContext.NpsSurveyResponses.AsNoTracking()
            .Where(response => projectIds.Contains(response.ProjectId))
            .Select(response => new NpsSnapshotResponse(
                response.ProjectId,
                response.DispatchId,
                response.SubmittedAt,
                response.Score,
                response.Classification))
            .ToListAsync(ct);
        var byProject = responses.GroupBy(response => response.ProjectId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<NpsSnapshotResponse>)group.ToList());

        var snapshots = projects.Select(project => new NpsProjectSnapshot(
            project,
            collections.GetValueOrDefault(project.Id),
            byProject.GetValueOrDefault(project.Id) ?? [])).ToList();

        if (!filter.IncludeWaived)
        {
            snapshots = snapshots.Where(snapshot => !snapshot.IsWaived).ToList();
        }

        return snapshots;
    }

    private IQueryable<SurveyResponse> ApplyResponseFilters(
        IQueryable<SurveyResponse> query,
        NpsFilter filter)
    {
        var projectQuery = NpsQueryFilters.ApplyProjectFilters(
            dbContext.Projects.AsNoTracking(),
            filter with { Search = null });
        query = query.Where(response => projectQuery.Select(project => project.Id).Contains(response.ProjectId));

        // Sem isto a aba Respostas e o CSV listavam respostas de projetos
        // dispensados que o dashboard e a tabela de resultados já não contavam,
        // para exatamente o mesmo estado de filtro.
        if (!filter.IncludeWaived)
        {
            query = query.Where(response => !dbContext.NpsCollections.Any(collection =>
                collection.ProjectId == response.ProjectId && collection.WaivedAt != null));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = NpsQueryFilters.SearchPattern(filter.Search);
            query = query.Where(response =>
                dbContext.Projects.Any(project =>
                    project.Id == response.ProjectId && EF.Functions.ILike(project.Name, pattern, NpsQueryFilters.SearchEscape)) ||
                (response.RespondentName != null && EF.Functions.ILike(response.RespondentName, pattern, NpsQueryFilters.SearchEscape)) ||
                (response.RespondentEmail != null && EF.Functions.ILike(response.RespondentEmail, pattern, NpsQueryFilters.SearchEscape)) ||
                (response.Comment != null && EF.Functions.ILike(response.Comment, pattern, NpsQueryFilters.SearchEscape)) ||
                (response.ContactId.HasValue && dbContext.NpsContacts.Any(contact =>
                    contact.Id == response.ContactId.Value &&
                    (EF.Functions.ILike(contact.Name, pattern, NpsQueryFilters.SearchEscape) ||
                        EF.Functions.ILike(contact.Email, pattern, NpsQueryFilters.SearchEscape)))));
        }

        query = NpsQueryFilters.ApplyResponsePeriodFilters(query, filter);

        if (filter.Formats.Count != 0)
        {
            var formats = filter.Formats.Select(NpsCodes.ParseFormat).ToArray();
            query = query.Where(response => formats.Contains(response.Format));
        }

        if (filter.Classifications.Count != 0)
        {
            var classifications = filter.Classifications.Select(NpsCodes.ParseClassification).ToArray();
            query = query.Where(response => classifications.Contains(response.Classification));
        }

        return query;
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
            var availability = NpsViewMappings.DispatchAvailability(dispatch, now);
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
                response.SubmittedAt,
                NpsCodes.ClassificationTone(response.Classification));
        }).ToArray();
    }
}
