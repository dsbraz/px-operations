namespace PxOperations.Application.Features.Nps.UseCases;

public sealed class ListNpsDispatchesUseCase(INpsRepository repository)
{
    public Task<IReadOnlyList<NpsDispatchView>> ExecuteAsync(int projectId, CancellationToken ct)
        => repository.ListDispatchesAsync(projectId, ct);
}
