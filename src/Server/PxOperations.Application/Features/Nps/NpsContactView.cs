namespace PxOperations.Application.Features.Nps;

public sealed record NpsContactView(
    int Id,
    int ProjectId,
    string Name,
    string Email,
    string? Role,
    bool IsArchived,
    string CreatedAt,
    string? ArchivedAt);
