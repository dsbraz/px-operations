using Microsoft.EntityFrameworkCore;
using PxOperations.Application.Features.Nps;
using PxOperations.Domain.Nps;
using PxOperations.Domain.Projects;

namespace PxOperations.Infrastructure.Features.Nps;

internal sealed record NpsSnapshotResponse(
    int ProjectId,
    int DispatchId,
    DateTimeOffset SubmittedAt,
    int Score,
    NpsClassification Classification);

internal sealed record NpsProjectSnapshot(
    Project Project,
    NpsCollection? Collection,
    IReadOnlyList<NpsSnapshotResponse> Responses)
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
