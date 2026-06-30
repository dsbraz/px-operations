namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record UpdateNpsContactRequest(
    string Name,
    string Email,
    string? Role);
