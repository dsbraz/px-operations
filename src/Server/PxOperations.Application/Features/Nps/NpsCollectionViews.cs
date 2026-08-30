namespace PxOperations.Application.Features.Nps;

public sealed record NpsTemporalView(string Label, string Tone, DateTimeOffset? At);

public sealed record NpsLinkView(
    int DispatchId,
    Guid Token,
    string Format,
    string FormatLabel,
    DateTimeOffset ExpiresAt,
    string Availability,
    string AvailabilityLabel,
    string Tone);

public sealed record NpsPrimaryActionView(
    string Code,
    string Label,
    string? Format,
    int? DispatchId,
    Guid? Token);

public sealed record NpsWaiverView(string Reason, DateTimeOffset WaivedAt);

public sealed record NpsProjectView(
    int Id,
    string Name,
    string? Client,
    string Dc,
    string? DeliveryManager,
    string ProjectType,
    int ResponsesCount,
    NpsBadgeView Stage,
    NpsTemporalView Temporal,
    NpsWaiverView? Waiver,
    IReadOnlyList<NpsLinkView> ActiveLinks,
    NpsPrimaryActionView? PrimaryAction,
    bool IsOverdue,
    DateTimeOffset? LastDispatchClosedAt);

public sealed record NpsProjectDetailView(
    NpsProjectView Project,
    decimal? OfficialNps,
    decimal? AverageScore,
    int ResponsesCount,
    int PromotersCount,
    IReadOnlyList<NpsLinkView> ActiveLinks,
    IReadOnlyList<NpsResponseView> RecentResponses);

public sealed record NpsContactView(
    int Id,
    int ProjectId,
    string Name,
    string Email,
    string? Role,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ArchivedAt);
