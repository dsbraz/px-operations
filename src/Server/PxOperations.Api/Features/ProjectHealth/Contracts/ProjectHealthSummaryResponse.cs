namespace PxOperations.Api.Features.ProjectHealth.Contracts;

public sealed record ProjectHealthSummaryResponse(
    int TotalEntries,
    int TotalProjects,
    double AverageScore,
    double OverallAverageScore,
    double AverageScope,
    double AverageSchedule,
    double AverageQuality,
    double AverageSatisfaction,
    int CriticalCount,
    int OverallCriticalCount,
    int AttentionCount,
    int HealthyCount,
    int NoResponseCount,
    int OverallNoResponseCount,
    int WithExpansionCount,
    int WithActionPlanCount,
    IReadOnlyList<WeeklyScorePointResponse> WeeklyEvolution);

public sealed record WeeklyScorePointResponse(
    string Week,
    double AverageScore,
    int EntryCount);
