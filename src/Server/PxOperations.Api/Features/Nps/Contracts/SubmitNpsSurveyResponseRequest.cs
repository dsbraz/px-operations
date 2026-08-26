namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record SubmitNpsSurveyResponseRequest(
    int Score,
    int? BusinessValue,
    int? Schedule,
    int? Quality,
    int? Communication,
    string? Tags,
    string? Comment,
    string? RespondentName,
    string? RespondentEmail);
