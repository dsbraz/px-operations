namespace PxOperations.Api.Features.Milestones.Contracts;

public sealed record MilestoneResponse(
    int Id,
    int ProjectId,
    string ProjectName,
    string? ProjectClient,
    string ProjectDc,
    string Type,
    string Title,
    string Date,
    string? Time,
    string? Notes);
