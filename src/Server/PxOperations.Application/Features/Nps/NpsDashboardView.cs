namespace PxOperations.Application.Features.Nps;

public sealed record NpsDashboardView(
    int TotalProjects,
    int OverdueProjects,
    int ActiveDispatches,
    int TotalResponses,
    decimal OfficialNps,
    decimal AverageScore,
    int Detractors,
    int Passives,
    int Promoters);
