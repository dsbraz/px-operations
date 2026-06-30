namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record NpsDispatchDetailResponse(
    NpsDispatchResponse Dispatch,
    IReadOnlyList<NpsDispatchTargetResponse> Targets);
