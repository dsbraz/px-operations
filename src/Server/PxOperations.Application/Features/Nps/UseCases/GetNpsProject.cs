namespace PxOperations.Application.Features.Nps.UseCases;

public sealed class GetNpsProjectUseCase(INpsRepository repository)
{
    public Task<NpsProjectDetailView?> ExecuteAsync(int projectId, CancellationToken ct)
        => repository.GetProjectAsync(projectId, ct);
}
