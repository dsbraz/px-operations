using PxOperations.Domain.Milestones;
using PxOperations.Domain.Projects;

namespace PxOperations.Application.Features.Milestones;

public sealed record MilestoneFilter(
    string? Search,
    DeliveryCenter? Dc,
    MilestoneType? Type,
    int? ProjectId,
    DateOnly? From,
    DateOnly? To);
