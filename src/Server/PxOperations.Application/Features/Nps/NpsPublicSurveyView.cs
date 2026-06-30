namespace PxOperations.Application.Features.Nps;

public sealed record NpsPublicSurveyView(
    Guid Token,
    int ProjectId,
    string ProjectName,
    int DispatchId,
    string PeriodStart,
    string PeriodEnd,
    string Format,
    string Language,
    bool AlreadyAnswered);
