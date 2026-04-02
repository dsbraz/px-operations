namespace PxOperations.Application.Features.ProjectHealth.UseCases;

public sealed class GetProjectHealthUseCase(IProjectHealthRepository repository)
{
    public async Task<ProjectHealthView?> ExecuteAsync(int id, CancellationToken ct)
    {
        return await repository.GetViewByIdAsync(id, ct);
    }
}
