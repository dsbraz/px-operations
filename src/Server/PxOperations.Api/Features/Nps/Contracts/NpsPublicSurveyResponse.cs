namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record NpsPublicSurveyResponse(
    Guid Token,
    int ProjectId,
    string ProjectName,
    int DispatchId,
    string PeriodStart,
    string PeriodEnd,
    string Format,
    string Language,
    bool AlreadyAnswered);
