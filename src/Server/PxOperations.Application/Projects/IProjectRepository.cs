using PxOperations.Domain.Projects;

namespace PxOperations.Application.Projects;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Project>> ListAsync(ProjectFilter filter, CancellationToken ct);
    void Add(Project project);
    void Remove(Project project);
}
