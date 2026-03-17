namespace PxOperations.Application.Features.Diagnostics;

public interface IReadinessService
{
    Task<ReadinessStatus> CheckAsync(CancellationToken cancellationToken = default);
}
