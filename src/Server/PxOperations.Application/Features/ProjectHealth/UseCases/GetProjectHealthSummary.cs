namespace PxOperations.Application.Features.ProjectHealth.UseCases;

public sealed class GetProjectHealthSummaryUseCase(IProjectHealthRepository repository)
{
    public async Task<ProjectHealthSummary> ExecuteAsync(ProjectHealthFilter filter, CancellationToken ct)
    {
        return await repository.GetSummaryAsync(filter, ct);
    }
}
