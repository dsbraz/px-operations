using Microsoft.EntityFrameworkCore;
using PxOperations.Application.Features.ProjectHealth;
using PxOperations.Domain.ProjectHealth;
using PxOperations.Domain.Projects;
using PxOperations.Infrastructure.Persistence;

namespace PxOperations.Infrastructure.Features.ProjectHealth;

public sealed class ProjectHealthRepository(AppDbContext dbContext) : IProjectHealthRepository
{
    public async Task<Domain.ProjectHealth.ProjectHealth?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await dbContext.ProjectHealth.FirstOrDefaultAsync(h => h.Id == id, ct);
    }

    public async Task<ProjectHealthView?> GetViewByIdAsync(int id, CancellationToken ct)
    {
        var projectHealth = await dbContext.ProjectHealth
            .Include(h => h.Project)
            .FirstOrDefaultAsync(h => h.Id == id, ct);

        return projectHealth is null ? null : ToView(projectHealth);
    }

    public async Task<IReadOnlyList<ProjectHealthView>> ListAsync(ProjectHealthFilter filter, CancellationToken ct)
    {
        IQueryable<Domain.ProjectHealth.ProjectHealth> query = dbContext.ProjectHealth.Include(h => h.Project);

        query = ApplyFilters(query, filter);

        var entries = await query
            .OrderByDescending(h => h.Week)
            .ThenBy(h => h.Project.Name)
            .ToListAsync(ct);

        return entries.Select(ToView).ToList();
    }

    public async Task<ProjectHealthSummary> GetSummaryAsync(ProjectHealthFilter filter, CancellationToken ct)
    {
        IQueryable<Domain.ProjectHealth.ProjectHealth> query = dbContext.ProjectHealth.Include(h => h.Project);
        query = ApplyFilters(query, filter);

        var entries = await query.ToListAsync(ct);

        var totalEntries = entries.Count;
        var projectIds = entries.Select(e => e.ProjectId).Distinct().ToList();
        var totalProjects = projectIds.Count;

        var avgScore = totalEntries > 0 ? entries.Average(e => e.Score) : 0;
        var avgScope = totalEntries > 0 ? entries.Average(e => RagScore(e.Scope)) : 0;
        var avgSchedule = totalEntries > 0 ? entries.Average(e => RagScore(e.Schedule)) : 0;
        var avgQuality = totalEntries > 0 ? entries.Average(e => RagScore(e.Quality)) : 0;
        var avgSatisfaction = totalEntries > 0 ? entries.Average(e => RagScore(e.Satisfaction)) : 0;

        var criticalCount = entries.Count(e => e.Score <= 3);
        var attentionCount = entries.Count(e => e.Score >= 4 && e.Score <= 6);
        var healthyCount = entries.Count(e => e.Score >= 7);

        // No-response: active projects without entry for the latest week
        var latestWeek = filter.Week ?? entries.Select(e => e.Week).DefaultIfEmpty().Max();
        var allProjectIds = await GetActiveProjectIds(filter, ct);
        var respondedProjectIds = latestWeek != default
            ? entries.Where(e => e.Week == latestWeek).Select(e => e.ProjectId).Distinct().ToHashSet()
            : new HashSet<int>();
        var noResponseCount = allProjectIds.Count(id => !respondedProjectIds.Contains(id));

        var withExpansionCount = entries.Select(e => e.ProjectId).Distinct()
            .Count(pid => entries.Any(e => e.ProjectId == pid && e.ExpansionOpportunity));
        var withActionPlanCount = entries.Select(e => e.ProjectId).Distinct()
            .Count(pid => entries.Any(e => e.ProjectId == pid && e.ActionPlanNeeded));

        var weeklyEvolution = entries
            .GroupBy(e => e.Week)
            .OrderBy(g => g.Key)
            .TakeLast(12)
            .Select(g => new WeeklyScorePoint(
                g.Key.ToString("yyyy-MM-dd"),
                Math.Round(g.Average(e => e.Score), 1),
                g.Count()))
            .ToList();

        return new ProjectHealthSummary(
            totalEntries,
            totalProjects,
            Math.Round(avgScore, 1),
            Math.Round(avgScope, 1),
            Math.Round(avgSchedule, 1),
            Math.Round(avgQuality, 1),
            Math.Round(avgSatisfaction, 1),
            criticalCount,
            attentionCount,
            healthyCount,
            noResponseCount,
            withExpansionCount,
            withActionPlanCount,
            weeklyEvolution);
    }

    public void Add(Domain.ProjectHealth.ProjectHealth projectHealth)
    {
        dbContext.ProjectHealth.Add(projectHealth);
    }

    public void Remove(Domain.ProjectHealth.ProjectHealth projectHealth)
    {
        dbContext.ProjectHealth.Remove(projectHealth);
    }

    private static IQueryable<Domain.ProjectHealth.ProjectHealth> ApplyFilters(IQueryable<Domain.ProjectHealth.ProjectHealth> query, ProjectHealthFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(h =>
                EF.Functions.ILike(h.Project.Name, $"%{filter.Search}%") ||
                (h.Project.Client != null && EF.Functions.ILike(h.Project.Client, $"%{filter.Search}%")) ||
                (h.SubProject != null && EF.Functions.ILike(h.SubProject, $"%{filter.Search}%")));
        }

        if (filter.Dc.HasValue)
            query = query.Where(h => h.Project.Dc == filter.Dc.Value);

        if (filter.ProjectId.HasValue)
            query = query.Where(h => h.ProjectId == filter.ProjectId.Value);

        if (filter.Week.HasValue)
            query = query.Where(h => h.Week == filter.Week.Value);

        if (filter.MinScore.HasValue)
            query = query.Where(h => h.Score >= filter.MinScore.Value);

        if (filter.MaxScore.HasValue)
            query = query.Where(h => h.Score <= filter.MaxScore.Value);

        return query;
    }

    private async Task<List<int>> GetActiveProjectIds(ProjectHealthFilter filter, CancellationToken ct)
    {
        IQueryable<Project> projectQuery = dbContext.Projects
            .Where(p => p.Status == ProjectStatus.InProgress);

        if (filter.Dc.HasValue)
            projectQuery = projectQuery.Where(p => p.Dc == filter.Dc.Value);

        return await projectQuery.Select(p => p.Id).ToListAsync(ct);
    }

    private static ProjectHealthView ToView(Domain.ProjectHealth.ProjectHealth projectHealth)
    {
        return new ProjectHealthView(
            projectHealth.Id,
            projectHealth.ProjectId,
            projectHealth.Project.Name,
            projectHealth.Project.Client,
            FormatDc(projectHealth.Project.Dc),
            projectHealth.Project.DeliveryManager,
            projectHealth.SubProject,
            projectHealth.Week.ToString("yyyy-MM-dd"),
            projectHealth.ReporterEmail,
            projectHealth.PracticesCount,
            FormatRag(projectHealth.Scope),
            FormatRag(projectHealth.Schedule),
            FormatRag(projectHealth.Quality),
            FormatRag(projectHealth.Satisfaction),
            projectHealth.Score,
            projectHealth.ExpansionOpportunity,
            projectHealth.ExpansionComment,
            projectHealth.ActionPlanNeeded,
            projectHealth.Highlights);
    }

    private static string FormatDc(DeliveryCenter dc) => dc switch
    {
        DeliveryCenter.Dc1 => "DC1",
        DeliveryCenter.Dc2 => "DC2",
        DeliveryCenter.Dc3 => "DC3",
        DeliveryCenter.Dc4 => "DC4",
        DeliveryCenter.Dc5 => "DC5",
        _ => "DC6"
    };

    private static string FormatRag(RagStatus status) => status switch
    {
        RagStatus.Green => "Verde",
        RagStatus.Yellow => "Amarelo",
        RagStatus.Red => "Vermelho",
        _ => "Verde"
    };

    private static double RagScore(RagStatus status) => status switch
    {
        RagStatus.Green => 2,
        RagStatus.Yellow => 1,
        RagStatus.Red => 0,
        _ => 0
    };
}
