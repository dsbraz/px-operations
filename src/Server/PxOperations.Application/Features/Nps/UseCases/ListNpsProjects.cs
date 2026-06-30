namespace PxOperations.Application.Features.Nps.UseCases;

public sealed class ListNpsProjectsUseCase(INpsRepository repository)
{
    public Task<IReadOnlyList<NpsProjectView>> ExecuteAsync(NpsFilter filter, CancellationToken ct)
        => repository.ListProjectsAsync(filter, ct);
}
