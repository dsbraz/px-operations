namespace PxOperations.Api.Features.Milestones.Contracts;

public sealed record UpdateMilestoneRequest(
    int? ProjectId = null,
    string? Type = null,
    string? Title = null,
    string? Date = null,
    string? Time = null,
    string? Notes = null);
