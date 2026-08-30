namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record SubmitNpsSurveyResponseRequest(
    int Score,
    int? Quality,
    int? Schedule,
    int? Communication,
    int? BusinessValue,
    string? Comment,
    string? RespondentName,
    string? RespondentEmail);
