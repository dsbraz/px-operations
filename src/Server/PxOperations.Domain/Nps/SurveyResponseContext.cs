namespace PxOperations.Domain.Nps;

public sealed record SurveyResponseContext(
    int ProjectId,
    int DispatchId,
    int TargetId,
    int? ContactId,
    NpsFormFormat Format,
    NpsDispatchStatus DispatchStatus,
    DateTimeOffset ExpiresAt,
    bool IsWaived,
    bool IsTargetUsed,
    bool HasDuplicateEmail);
