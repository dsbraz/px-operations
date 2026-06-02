using System.Collections.Generic;

namespace PxOperations.Application.Features.ProjectHealth;

public sealed record ProjectHealthSummary(
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
    IReadOnlyList<WeeklyScorePoint> WeeklyEvolution);

public sealed record WeeklyScorePoint(
    string Week,
    double AverageScore,
    int EntryCount);
