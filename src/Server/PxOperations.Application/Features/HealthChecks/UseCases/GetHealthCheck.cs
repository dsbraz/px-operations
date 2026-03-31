namespace PxOperations.Application.Features.HealthChecks.UseCases;

public sealed class GetHealthCheckUseCase(IHealthCheckRepository repository)
{
    public async Task<HealthCheckView?> ExecuteAsync(int id, CancellationToken ct)
    {
        return await repository.GetViewByIdAsync(id, ct);
    }
}
