using PxOperations.Application.Abstractions;

namespace PxOperations.Application.Features.ProjectHealth.UseCases;

public sealed class DeleteProjectHealthUseCase(
    IProjectHealthRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<bool> ExecuteAsync(int id, CancellationToken ct)
    {
        var projectHealth = await repository.GetByIdAsync(id, ct);
        if (projectHealth is null) return false;

        repository.Remove(projectHealth);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
