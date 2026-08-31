namespace PxOperations.Application.Features.Nps;

public sealed record NpsDispatchView(
    int Id,
    int ProjectId,
    string ProjectName,
    string Format,
    string FormatLabel,
    string Language,
    string LanguageLabel,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ClosedAt,
    int TargetsCount,
    int ResponsesCount,
    string Availability,
    string AvailabilityLabel,
    string Tone);

public sealed record NpsDispatchTargetView(
    int Id,
    int DispatchId,
    int? ContactId,
    string? ContactName,
    string? ContactEmail,
    Guid Token,
    bool IsGeneric,
    int ResponsesCount);

public sealed record NpsDispatchDetailView(
    NpsDispatchView Dispatch,
    IReadOnlyList<NpsDispatchTargetView> Targets);
