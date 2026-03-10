using PxOperations.Domain.Projects;

namespace PxOperations.Application.Projects.UseCases;

public sealed class GetProjectUseCase(IProjectRepository repository)
{
    public async Task<Project?> ExecuteAsync(int id, CancellationToken ct)
    {
        return await repository.GetByIdAsync(id, ct);
    }
}
