namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record CreateNpsDispatchRequest(
    int ProjectId,
    string PeriodStart,
    string PeriodEnd,
    string Format,
    string Language,
    string CreatedBy,
    IReadOnlyList<int>? ContactIds,
    bool CreateGenericToken);
