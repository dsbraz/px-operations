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

public sealed record NpsAspectAverageView(
    string Code,
    string Label,
    decimal? Average,
    int ResponsesCount);

public sealed record NpsAspectSummaryView(
    int CompleteResponsesCount,
    NpsScaleView Scale,
    IReadOnlyList<NpsAspectAverageView> Aspects);

public sealed record NpsDashboardView(
    decimal? OfficialNps,
    int TotalResponses,
    decimal? AverageScore,
    int OverdueProjects,
    NpsScaleView Scale,
    IReadOnlyList<NpsDistributionView> Distribution,
    NpsAspectSummaryView AspectSummary,
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
