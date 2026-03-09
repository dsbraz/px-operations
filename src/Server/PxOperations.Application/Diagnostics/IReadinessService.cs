namespace PxOperations.Application.Diagnostics;

public interface IReadinessService
{
    Task<ReadinessStatus> CheckAsync(CancellationToken cancellationToken = default);
}
