namespace PxOperations.Application.Features.Milestones.UseCases;

public sealed class GetMilestoneUseCase(IMilestoneRepository repository)
{
    public async Task<MilestoneView?> ExecuteAsync(int id, CancellationToken ct)
    {
        return await repository.GetViewByIdAsync(id, ct);
    }
}
