using PxOperations.Domain.Projects;

namespace PxOperations.Application.Features.Projects.UseCases;

public sealed class ListProjectsUseCase(IProjectRepository repository)
{
    public async Task<IReadOnlyList<Project>> ExecuteAsync(
        ProjectFilter filter, CancellationToken ct)
    {
        return await repository.ListAsync(filter, ct);
    }
}
