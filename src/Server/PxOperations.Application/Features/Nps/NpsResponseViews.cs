namespace PxOperations.Application.Features.Nps;

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
