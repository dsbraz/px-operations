namespace PxOperations.Application.Features.Nps.UseCases;

public sealed class ListNpsContactsUseCase(INpsRepository repository)
{
    public Task<IReadOnlyList<NpsContactView>> ExecuteAsync(int projectId, bool includeArchived, CancellationToken ct)
        => repository.ListContactsAsync(projectId, includeArchived, ct);
}
