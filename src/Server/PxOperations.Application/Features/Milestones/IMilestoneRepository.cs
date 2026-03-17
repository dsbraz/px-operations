using PxOperations.Domain.Milestones;

namespace PxOperations.Application.Features.Milestones;

public interface IMilestoneRepository
{
    Task<Milestone?> GetByIdAsync(int id, CancellationToken ct);
    Task<MilestoneView?> GetViewByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<MilestoneView>> ListAsync(MilestoneFilter filter, CancellationToken ct);
    void Add(Milestone milestone);
    void Remove(Milestone milestone);
}
