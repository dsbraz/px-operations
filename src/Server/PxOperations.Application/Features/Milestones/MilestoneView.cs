namespace PxOperations.Application.Features.Milestones;

public sealed record MilestoneView(
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
