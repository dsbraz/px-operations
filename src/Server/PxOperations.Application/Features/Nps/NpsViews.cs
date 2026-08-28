namespace PxOperations.Application.Features.Nps;

public sealed record NpsOptionView(string Code, string Label);

public sealed record NpsFilterOptionsView(
    IReadOnlyList<NpsOptionView> Clients,
    IReadOnlyList<NpsOptionView> Dcs,
    IReadOnlyList<NpsOptionView> ProjectTypes,
    IReadOnlyList<NpsOptionView> DeliveryManagers,
    IReadOnlyList<NpsOptionView> Statuses,
    IReadOnlyList<NpsOptionView> Formats,
    IReadOnlyList<NpsOptionView> Classifications);

public sealed record NpsScaleView(int Minimum, int Maximum);

public sealed record NpsDistributionView(
    string Code,
    string Label,
    string Tone,
    int Count,
    decimal Percentage);

public sealed record NpsDashboardView(
    decimal? OfficialNps,
    int TotalResponses,
    decimal? AverageScore,
    int OverdueProjects,
    NpsScaleView Scale,
    IReadOnlyList<NpsDistributionView> Distribution,
    NpsFilterOptionsView FilterOptions);

public sealed record NpsBadgeView(string Code, string Label, string Tone);

public sealed record NpsFormatCountView(string Code, string Label, int Count);

public sealed record NpsProjectResultView(
    int Id,
    string Name,
    string? Client,
    string Dc,
    string? DeliveryManager,
    int ResponsesCount,
    decimal? OfficialNps,
    IReadOnlyList<NpsDistributionView> Distribution,
    IReadOnlyList<NpsFormatCountView> Formats,
    DateTimeOffset? LastResponseAt,
    NpsBadgeView Status);

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

public sealed record NpsResponseView(
    int Id,
    int ProjectId,
    string ProjectName,
    int DispatchId,
    int TargetId,
    int? ContactId,
    string? ContactName,
    string? ContactEmail,
    string Format,
    string FormatLabel,
    int Score,
    string Classification,
    string ClassificationLabel,
    int? Quality,
    int? Schedule,
    int? Communication,
    int? BusinessValue,
    string? Comment,
    string? RespondentName,
    string? RespondentEmail,
    DateTimeOffset SubmittedAt);

public sealed record NpsAspectView(string Code, string Label, NpsScaleView Scale);

public sealed record NpsPublicSurveyView(
    Guid Token,
    int ProjectId,
    string ProjectName,
    string? Client,
    int DispatchId,
    string Format,
    string Language,
    DateTimeOffset ExpiresAt,
    string Availability,
    bool IsGeneric,
    NpsScaleView ScoreScale,
    IReadOnlyList<NpsAspectView> Aspects);
