using Microsoft.EntityFrameworkCore;
using PxOperations.Application.Features.Milestones;
using PxOperations.Domain.Milestones;
using PxOperations.Domain.Projects;
using PxOperations.Infrastructure.Persistence;

namespace PxOperations.Infrastructure.Features.Milestones;

public sealed class MilestoneRepository(AppDbContext dbContext) : IMilestoneRepository
{
    public async Task<Milestone?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await dbContext.Milestones.FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<MilestoneView?> GetViewByIdAsync(int id, CancellationToken ct)
    {
        var milestone = await dbContext.Milestones
            .Include(m => m.Project)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        return milestone is null ? null : ToView(milestone);
    }

    public async Task<IReadOnlyList<MilestoneView>> ListAsync(MilestoneFilter filter, CancellationToken ct)
    {
        IQueryable<Milestone> query = dbContext.Milestones.Include(m => m.Project);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(m =>
                EF.Functions.ILike(m.Title, $"%{filter.Search}%") ||
                EF.Functions.ILike(m.Project.Name, $"%{filter.Search}%"));
        }

        if (filter.Dc.HasValue)
            query = query.Where(m => m.Project.Dc == filter.Dc.Value);

        if (filter.Type.HasValue)
            query = query.Where(m => m.Type == filter.Type.Value);

        if (filter.ProjectId.HasValue)
            query = query.Where(m => m.ProjectId == filter.ProjectId.Value);

        if (filter.From.HasValue)
            query = query.Where(m => m.Date >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(m => m.Date <= filter.To.Value);

        var milestones = await query
            .OrderBy(m => m.Date)
            .ThenBy(m => m.Time)
            .ThenBy(m => m.Title)
            .ToListAsync(ct);

        return milestones.Select(ToView).ToList();
    }

    public void Add(Milestone milestone)
    {
        dbContext.Milestones.Add(milestone);
    }

    public void Remove(Milestone milestone)
    {
        dbContext.Milestones.Remove(milestone);
    }

    private static MilestoneView ToView(Milestone milestone)
    {
        return new MilestoneView(
            milestone.Id,
            milestone.ProjectId,
            milestone.Project.Name,
            milestone.Project.Client,
            milestone.Project.Dc switch
            {
                DeliveryCenter.Dc1 => "DC1",
                DeliveryCenter.Dc2 => "DC2",
                DeliveryCenter.Dc3 => "DC3",
                DeliveryCenter.Dc4 => "DC4",
                DeliveryCenter.Dc5 => "DC5",
                _ => "DC6"
            },
            milestone.Type switch
            {
                MilestoneType.SponsorPresentation => "Apresentação Sponsor",
                MilestoneType.FinalDelivery => "Entrega Final",
                MilestoneType.ClientOnsite => "Presencial com Cliente",
                MilestoneType.Kickoff => "Kickoff",
                _ => "Outros"
            },
            milestone.Title,
            milestone.Date.ToString("yyyy-MM-dd"),
            milestone.Time.HasValue ? milestone.Time.Value.ToString("HH\\:mm") : null,
            milestone.Notes);
    }
}
