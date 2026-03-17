namespace PxOperations.Api.Features.Milestones.Contracts;

public sealed record CreateMilestoneRequest(
    int ProjectId,
    string Type,
    string Title,
    string Date,
    string? Time,
    string? Notes);
