namespace PxOperations.Domain.Nps;

public sealed record NpsOpenDispatchState(
    int DispatchId,
    NpsFormFormat Format,
    DateTimeOffset ExpiresAt,
    bool HasResponses);
