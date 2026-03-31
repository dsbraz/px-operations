using PxOperations.Domain.HealthChecks;

namespace PxOperations.Application.Features.HealthChecks;

public interface IHealthCheckRepository
{
    Task<HealthCheck?> GetByIdAsync(int id, CancellationToken ct);
    Task<HealthCheckView?> GetViewByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<HealthCheckView>> ListAsync(HealthCheckFilter filter, CancellationToken ct);
    Task<HealthCheckSummary> GetSummaryAsync(HealthCheckFilter filter, CancellationToken ct);
    void Add(HealthCheck healthCheck);
    void Remove(HealthCheck healthCheck);
}
