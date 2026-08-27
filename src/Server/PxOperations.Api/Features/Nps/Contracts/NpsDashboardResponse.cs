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
    int Promoters,
    int CompleteResponses,
    decimal? QualityAverage, int QualityCount,
    decimal? ScheduleAverage, int ScheduleCount,
    decimal? CommunicationAverage, int CommunicationCount,
    decimal? BusinessValueAverage, int BusinessValueCount);
