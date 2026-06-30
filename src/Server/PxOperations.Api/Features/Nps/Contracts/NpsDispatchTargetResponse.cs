namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record NpsDispatchTargetResponse(
    int Id,
    int DispatchId,
    int? ContactId,
    string? ContactName,
    string? ContactEmail,
    Guid Token,
    bool IsGeneric,
    int ResponsesCount);
