namespace PxOperations.Application.Features.ProjectHealth.UseCases;

public sealed class ListProjectHealthUseCase(IProjectHealthRepository repository)
{
    public async Task<IReadOnlyList<ProjectHealthView>> ExecuteAsync(ProjectHealthFilter filter, CancellationToken ct)
    {
        return await repository.ListAsync(filter, ct);
    }
}
