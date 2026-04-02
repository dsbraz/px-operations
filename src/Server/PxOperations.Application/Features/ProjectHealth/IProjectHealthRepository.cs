using PxOperations.Domain.ProjectHealth;

namespace PxOperations.Application.Features.ProjectHealth;

public interface IProjectHealthRepository
{
    Task<Domain.ProjectHealth.ProjectHealth?> GetByIdAsync(int id, CancellationToken ct);
    Task<ProjectHealthView?> GetViewByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<ProjectHealthView>> ListAsync(ProjectHealthFilter filter, CancellationToken ct);
    Task<ProjectHealthSummary> GetSummaryAsync(ProjectHealthFilter filter, CancellationToken ct);
    void Add(Domain.ProjectHealth.ProjectHealth projectHealth);
    void Remove(Domain.ProjectHealth.ProjectHealth projectHealth);
}
