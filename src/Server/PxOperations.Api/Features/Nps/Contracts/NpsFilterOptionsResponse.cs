namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record NpsFilterOptionsResponse(
    IReadOnlyList<string> Companies,
    IReadOnlyList<string> DeliveryManagers);
