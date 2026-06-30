namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record CreateNpsContactRequest(
    string Name,
    string Email,
    string? Role);
