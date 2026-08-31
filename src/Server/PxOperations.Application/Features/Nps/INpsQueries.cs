namespace PxOperations.Application.Features.Nps;

public interface INpsQueries
{
    Task<NpsDashboardView> GetDashboardAsync(NpsFilter filter, DateTimeOffset now, CancellationToken ct);
    Task<NpsFilterOptionsView> GetFilterOptionsAsync(CancellationToken ct);
    Task<IReadOnlyList<NpsProjectResultView>> ListProjectResultsAsync(NpsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<NpsResponseView>> ListResponsesAsync(NpsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<NpsProjectView>> ListProjectsAsync(NpsFilter filter, DateTimeOffset now, CancellationToken ct);
    Task<NpsProjectDetailView?> GetProjectAsync(int projectId, DateTimeOffset now, CancellationToken ct);
    Task<IReadOnlyList<NpsResponseView>> ListProjectResponsesAsync(int projectId, NpsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<NpsContactView>> ListContactsAsync(int projectId, bool includeArchived, CancellationToken ct);
    Task<NpsContactView?> GetContactAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<NpsDispatchView>> ListDispatchesAsync(int projectId, DateTimeOffset now, CancellationToken ct);
    Task<NpsDispatchDetailView?> GetDispatchAsync(int id, DateTimeOffset now, CancellationToken ct);
    Task<IReadOnlyList<NpsResponseView>> ListDispatchResponsesAsync(int dispatchId, CancellationToken ct);
    Task<NpsPublicSurveyView?> GetPublicSurveyAsync(Guid token, DateTimeOffset now, CancellationToken ct);
    Task<NpsResponseView?> GetResponseAsync(int id, CancellationToken ct);
}
