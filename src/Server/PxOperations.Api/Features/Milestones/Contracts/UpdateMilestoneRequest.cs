using PxOperations.Domain.Abstractions;

namespace PxOperations.Api.Features.Milestones.Contracts;

public sealed class UpdateMilestoneRequest
{
    public Optional<int> ProjectId { get; init; }
    public Optional<string> Type { get; init; }
    public Optional<string> Title { get; init; }
    public Optional<string> Date { get; init; }
    public Optional<string?> Time { get; init; }
    public Optional<string?> Notes { get; init; }
}
