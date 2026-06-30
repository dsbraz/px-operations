namespace PxOperations.Application.Features.Nps;

public sealed record NpsDispatchTargetView(
    int Id,
    int DispatchId,
    int? ContactId,
    string? ContactName,
    string? ContactEmail,
    Guid Token,
    bool IsGeneric,
    int ResponsesCount);
