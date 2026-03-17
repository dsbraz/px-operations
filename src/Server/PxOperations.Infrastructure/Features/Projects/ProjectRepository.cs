using Microsoft.EntityFrameworkCore;
using PxOperations.Application.Features.Projects;
using PxOperations.Domain.Projects;
using PxOperations.Infrastructure.Persistence;

namespace PxOperations.Infrastructure.Features.Projects;

public sealed class ProjectRepository(AppDbContext dbContext) : IProjectRepository
{
    public async Task<Project?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken ct)
    {
        return await dbContext.Projects.AnyAsync(p => p.Id == id, ct);
    }

    public async Task<IReadOnlyList<Project>> ListAsync(ProjectFilter filter, CancellationToken ct)
    {
        IQueryable<Project> query = dbContext.Projects;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, $"%{filter.Search}%") ||
                (p.Client != null && EF.Functions.ILike(p.Client, $"%{filter.Search}%")));
        }

        if (filter.Dc.HasValue)
            query = query.Where(p => p.Dc == filter.Dc.Value);

        if (filter.Status.HasValue)
            query = query.Where(p => p.Status == filter.Status.Value);

        if (filter.Type.HasValue)
            query = query.Where(p => p.Type == filter.Type.Value);

        if (filter.Renewal.HasValue)
            query = query.Where(p => p.Renewal == filter.Renewal.Value);

        return await query.OrderBy(p => p.Name).ToListAsync(ct);
    }

    public void Add(Project project)
    {
        dbContext.Projects.Add(project);
    }

    public void Remove(Project project)
    {
        dbContext.Projects.Remove(project);
    }
}
