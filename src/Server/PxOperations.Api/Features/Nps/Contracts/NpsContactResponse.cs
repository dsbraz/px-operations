namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record NpsContactResponse(
    int Id,
    int ProjectId,
    string Name,
    string Email,
    string? Role,
    bool IsArchived,
    string CreatedAt,
    string? ArchivedAt);
