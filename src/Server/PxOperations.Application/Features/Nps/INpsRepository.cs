using PxOperations.Domain.Nps;

namespace PxOperations.Application.Features.Nps;

public interface INpsRepository
{
    Task<bool> ProjectExistsAsync(int projectId, CancellationToken ct);
    Task<bool> ContactBelongsToProjectAsync(int projectId, int contactId, CancellationToken ct);
    Task<NpsDashboardView> GetDashboardAsync(NpsFilter filter, CancellationToken ct);
    Task<IReadOnlyList<NpsProjectView>> ListProjectsAsync(NpsFilter filter, CancellationToken ct);
    Task<NpsProjectDetailView?> GetProjectAsync(int projectId, CancellationToken ct);
    Task<IReadOnlyList<NpsContactView>> ListContactsAsync(int projectId, bool includeArchived, CancellationToken ct);
    Task<Contact?> GetContactAsync(int id, CancellationToken ct);
    void AddContact(Contact contact);
    Task<IReadOnlyList<NpsDispatchView>> ListDispatchesAsync(int projectId, CancellationToken ct);
    Task<NpsDispatchDetailView?> GetDispatchAsync(int id, CancellationToken ct);
    Task<Dispatch?> GetDispatchEntityAsync(int id, CancellationToken ct);
    void AddDispatch(Dispatch dispatch);
    void AddDispatchTarget(DispatchTarget target);
    Task<DispatchTarget?> GetTargetByTokenAsync(Guid token, CancellationToken ct);
    Task<bool> TargetHasResponseAsync(int targetId, CancellationToken ct);
    Task<NpsPublicSurveyView?> GetPublicSurveyAsync(Guid token, CancellationToken ct);
    Task<IReadOnlyList<NpsResponseView>> ListResponsesAsync(int? dispatchId, NpsFilter filter, CancellationToken ct);
    void AddResponse(SurveyResponse response);
    bool IsDuplicateResponseException(Exception exception);
}
