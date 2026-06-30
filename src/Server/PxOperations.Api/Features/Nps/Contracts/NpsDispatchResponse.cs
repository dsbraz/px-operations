namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record NpsDispatchResponse(
    int Id,
    int ProjectId,
    string ProjectName,
    string PeriodStart,
    string PeriodEnd,
    string Format,
    string Language,
    string Status,
    string CreatedBy,
    string CreatedAt,
    string? ClosedAt,
    int TargetsCount,
    int ResponsesCount);
