namespace PxOperations.Application.Features.Nps.UseCases;

public sealed class ListNpsResponsesUseCase(INpsRepository repository)
{
    public Task<IReadOnlyList<NpsResponseView>> ExecuteAsync(int? dispatchId, NpsFilter filter, CancellationToken ct)
        => repository.ListResponsesAsync(dispatchId, filter, ct);
}
