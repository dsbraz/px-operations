namespace PxOperations.Application.Features.Nps;

public sealed record NpsDispatchView(
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
    string ExpiresAt,
    bool IsExpired,
    int TargetsCount,
    int ResponsesCount);
