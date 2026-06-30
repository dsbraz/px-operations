namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record NpsSurveyResponse(
    int Id,
    int ProjectId,
    string ProjectName,
    int DispatchId,
    int TargetId,
    int? ContactId,
    string? ContactName,
    string? ContactEmail,
    int Score,
    string Classification,
    int? Scope,
    int? Schedule,
    int? Quality,
    int? Communication,
    string? Tags,
    string? Comment,
    string? RespondentName,
    string? RespondentEmail,
    string SubmittedAt);
