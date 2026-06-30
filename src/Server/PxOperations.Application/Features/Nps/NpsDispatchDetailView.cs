namespace PxOperations.Application.Features.Nps;

public sealed record NpsDispatchDetailView(
    NpsDispatchView Dispatch,
    IReadOnlyList<NpsDispatchTargetView> Targets);
