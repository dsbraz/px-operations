namespace PxOperations.Application.Features.Milestones.UseCases;

public sealed class ListMilestonesUseCase(IMilestoneRepository repository)
{
    public async Task<IReadOnlyList<MilestoneView>> ExecuteAsync(MilestoneFilter filter, CancellationToken ct)
    {
        return await repository.ListAsync(filter, ct);
    }
}
