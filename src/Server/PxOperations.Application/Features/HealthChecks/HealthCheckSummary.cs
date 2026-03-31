namespace PxOperations.Application.Features.HealthChecks;

public sealed record HealthCheckSummary(
    int TotalEntries,
    int TotalProjects,
    double AverageScore,
    double AverageScope,
    double AverageSchedule,
    double AverageQuality,
    double AverageSatisfaction,
    int CriticalCount,
    int AttentionCount,
    int HealthyCount,
    int NoResponseCount,
    int WithExpansionCount,
    int WithActionPlanCount,
    IReadOnlyList<WeeklyScorePoint> WeeklyEvolution);

public sealed record WeeklyScorePoint(
    string Week,
    double AverageScore,
    int EntryCount);
