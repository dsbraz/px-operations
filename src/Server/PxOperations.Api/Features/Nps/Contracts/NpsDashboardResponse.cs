namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record NpsDashboardResponse(
    int TotalProjects,
    int OverdueProjects,
    int ActiveDispatches,
    int TotalResponses,
    decimal OfficialNps,
    decimal AverageScore,
    int Detractors,
    int Passives,
    int Promoters);
