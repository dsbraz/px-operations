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
        // The carteira = active projects (InProgress, optionally scoped by DC).
        var activeProjectIds = (await GetActiveProjectIds(filter, ct)).ToHashSet();
        var totalProjects = activeProjectIds.Count;

        // Weekly evolution = trend over the 12 most recent weeks, aggregated in the database
        // (no row hydration). Independent of the selected period.
        var weekly = await dbContext.ProjectHealth
            .Where(h => activeProjectIds.Contains(h.ProjectId))
            .GroupBy(h => h.Week)
            .Select(g => new { Week = g.Key, AverageScore = g.Average(x => x.Score), EntryCount = g.Count() })
            .OrderByDescending(w => w.Week)
            .Take(12)
            .ToListAsync(ct);

        var weeklyEvolution = weekly
            .OrderBy(w => w.Week)
            .Select(w => new WeeklyScorePoint(
                w.Week.ToString("yyyy-MM-dd"),
                Math.Round(w.AverageScore, 1),
                w.EntryCount))
            .ToList();

        // Most recent week with data, ignoring the period filter — anchors the fixed top counters.
        var globalLatestWeek = weekly.Select(w => w.Week).DefaultIfEmpty().Max();

        // Selected period = explicit filter week, or the most recent week.
        var latestWeek = filter.Week ?? globalLatestWeek;

        // Week snapshot = the period's responses (selected week, or the most recent). Drives the
        // operational counters (critical/attention/healthy, no-response, expansion/action plan).
        var snapshot = latestWeek != default
            ? await dbContext.ProjectHealth
                .Where(h => activeProjectIds.Contains(h.ProjectId) && h.Week == latestWeek)
                .ToListAsync(ct)
            : new List<Domain.ProjectHealth.ProjectHealth>();

        var totalEntries = snapshot.Count;

        // Overall = every response of every active project, across all weeks (DB-side aggregate,
        // no row hydration). Independent of the period/score filters. Feeds the fixed top "Média"
        // and the default of "Saúde Geral" when no specific week is selected.
        var overall = await dbContext.ProjectHealth
            .Where(h => activeProjectIds.Contains(h.ProjectId))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Score = g.Average(x => (double)x.Score),
                Scope = g.Average(x => x.Scope == RagStatus.Green ? 2.0 : x.Scope == RagStatus.Yellow ? 1.0 : 0.0),
                Schedule = g.Average(x => x.Schedule == RagStatus.Green ? 2.0 : x.Schedule == RagStatus.Yellow ? 1.0 : 0.0),
                Quality = g.Average(x => x.Quality == RagStatus.Green ? 2.0 : x.Quality == RagStatus.Yellow ? 1.0 : 0.0),
                Satisfaction = g.Average(x => x.Satisfaction == RagStatus.Green ? 2.0 : x.Satisfaction == RagStatus.Yellow ? 1.0 : 0.0),
            })
            .FirstOrDefaultAsync(ct);

        var overallScore = overall?.Score ?? 0;

        // "Saúde Geral" (score + dimensions): the selected week when filtering by period,
        // otherwise the overall average of all active projects.
        var hasWeekFilter = filter.Week is not null;
        var avgScore = hasWeekFilter ? (totalEntries > 0 ? snapshot.Average(e => e.Score) : 0) : overallScore;
        var avgScope = hasWeekFilter ? (totalEntries > 0 ? snapshot.Average(e => RagScore(e.Scope)) : 0) : (overall?.Scope ?? 0);
        var avgSchedule = hasWeekFilter ? (totalEntries > 0 ? snapshot.Average(e => RagScore(e.Schedule)) : 0) : (overall?.Schedule ?? 0);
        var avgQuality = hasWeekFilter ? (totalEntries > 0 ? snapshot.Average(e => RagScore(e.Quality)) : 0) : (overall?.Quality ?? 0);
        var avgSatisfaction = hasWeekFilter ? (totalEntries > 0 ? snapshot.Average(e => RagScore(e.Satisfaction)) : 0) : (overall?.Satisfaction ?? 0);

        var criticalCount = snapshot.Count(e => e.Score <= 3);
        var attentionCount = snapshot.Count(e => e.Score >= 4 && e.Score <= 6);
        var healthyCount = snapshot.Count(e => e.Score >= 7);

        // No-response: active projects without an entry in the selected period.
        var respondedProjectIds = snapshot.Select(e => e.ProjectId).Distinct().ToHashSet();
        var noResponseCount = totalProjects - respondedProjectIds.Count;

        var withExpansionCount = snapshot.Where(e => e.ExpansionOpportunity).Select(e => e.ProjectId).Distinct().Count();
        var withActionPlanCount = snapshot.Where(e => e.ActionPlanNeeded).Select(e => e.ProjectId).Distinct().Count();

        // Fixed top counters = current state at the most recent week, regardless of the period
        // filter. When no week is selected the global snapshot equals the selected one.
        var globalSnapshot = filter.Week is null
            ? snapshot
            : (globalLatestWeek != default
                ? await dbContext.ProjectHealth
                    .Where(h => activeProjectIds.Contains(h.ProjectId) && h.Week == globalLatestWeek)
                    .ToListAsync(ct)
                : new List<Domain.ProjectHealth.ProjectHealth>());

        var overallCriticalCount = globalSnapshot.Count(e => e.Score <= 3);
        var overallNoResponseCount = totalProjects - globalSnapshot.Select(e => e.ProjectId).Distinct().Count();

        return new ProjectHealthSummary(
            totalEntries,
            totalProjects,
            Math.Round(avgScore, 1),
            Math.Round(overallScore, 1),
            Math.Round(avgScope, 1),
            Math.Round(avgSchedule, 1),
            Math.Round(avgQuality, 1),
            Math.Round(avgSatisfaction, 1),
            criticalCount,
            overallCriticalCount,
            attentionCount,
            healthyCount,
            noResponseCount,
            overallNoResponseCount,
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
            // Escape LIKE wildcards so a literal %/_ in the search term is matched literally, not as a pattern.
            var term = EscapeLikePattern(filter.Search);
            query = query.Where(h =>
                EF.Functions.ILike(h.Project.Name, $"%{term}%", "\\") ||
                (h.Project.Client != null && EF.Functions.ILike(h.Project.Client, $"%{term}%", "\\")) ||
                (h.SubProject != null && EF.Functions.ILike(h.SubProject, $"%{term}%", "\\")));
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

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\")
        .Replace("%", "\\%")
        .Replace("_", "\\_");

    private async Task<List<int>> GetActiveProjectIds(ProjectHealthFilter filter, CancellationToken ct)
    {
        IQueryable<Project> projectQuery = dbContext.Projects
            .Where(p => p.Status == ProjectStatus.InProgress);

        if (filter.Dc.HasValue)
            projectQuery = projectQuery.Where(p => p.Dc == filter.Dc.Value);

        if (filter.ProjectId.HasValue)
            projectQuery = projectQuery.Where(p => p.Id == filter.ProjectId.Value);

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
            projectHealth.ActionPlanComment,
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
