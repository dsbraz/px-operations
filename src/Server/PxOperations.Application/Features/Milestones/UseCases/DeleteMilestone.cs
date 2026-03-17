using PxOperations.Application.Abstractions;

namespace PxOperations.Application.Features.Milestones.UseCases;

public sealed class DeleteMilestoneUseCase(
    IMilestoneRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<bool> ExecuteAsync(int id, CancellationToken ct)
    {
        var milestone = await repository.GetByIdAsync(id, ct);
        if (milestone is null) return false;

        repository.Remove(milestone);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
