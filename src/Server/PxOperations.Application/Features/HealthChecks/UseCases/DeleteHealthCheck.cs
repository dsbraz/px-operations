using PxOperations.Application.Abstractions;

namespace PxOperations.Application.Features.HealthChecks.UseCases;

public sealed class DeleteHealthCheckUseCase(
    IHealthCheckRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<bool> ExecuteAsync(int id, CancellationToken ct)
    {
        var healthCheck = await repository.GetByIdAsync(id, ct);
        if (healthCheck is null) return false;

        repository.Remove(healthCheck);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
