using PxOperations.Application.Abstractions;

namespace PxOperations.Application.Features.Projects.UseCases;

public sealed class DeleteProjectUseCase(
    IProjectRepository repository,
    IUnitOfWork unitOfWork)
{
    public async Task<bool> ExecuteAsync(int id, CancellationToken ct)
    {
        var project = await repository.GetByIdAsync(id, ct);
        if (project is null) return false;

        repository.Remove(project);
        await unitOfWork.SaveChangesAsync(ct);
        return true;
    }
}
