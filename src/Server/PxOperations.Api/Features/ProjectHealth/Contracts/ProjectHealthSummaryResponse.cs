namespace PxOperations.Api.Features.ProjectHealth.Contracts;

public sealed record ProjectHealthSummaryResponse(
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
    IReadOnlyList<WeeklyScorePointResponse> WeeklyEvolution);

public sealed record WeeklyScorePointResponse(
    string Week,
    double AverageScore,
    int EntryCount);
