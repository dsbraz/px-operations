namespace PxOperations.Application.Features.HealthChecks.UseCases;

public sealed class GetHealthCheckSummaryUseCase(IHealthCheckRepository repository)
{
    public async Task<HealthCheckSummary> ExecuteAsync(HealthCheckFilter filter, CancellationToken ct)
    {
        return await repository.GetSummaryAsync(filter, ct);
    }
}
