namespace PxOperations.Application.Features.HealthChecks.UseCases;

public sealed class ListHealthChecksUseCase(IHealthCheckRepository repository)
{
    public async Task<IReadOnlyList<HealthCheckView>> ExecuteAsync(HealthCheckFilter filter, CancellationToken ct)
    {
        return await repository.ListAsync(filter, ct);
    }
}
