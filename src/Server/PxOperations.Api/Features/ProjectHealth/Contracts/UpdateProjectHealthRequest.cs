using PxOperations.Domain.Abstractions;

namespace PxOperations.Api.Features.ProjectHealth.Contracts;

public sealed class UpdateProjectHealthRequest
{
    public Optional<int> ProjectId { get; init; }
    public Optional<string?> SubProject { get; init; }
    public Optional<string> Week { get; init; }
    public Optional<string> ReporterEmail { get; init; }
    public Optional<int> PracticesCount { get; init; }
    public Optional<string> Scope { get; init; }
    public Optional<string> Schedule { get; init; }
    public Optional<string> Quality { get; init; }
    public Optional<string> Satisfaction { get; init; }
    public Optional<bool> ExpansionOpportunity { get; init; }
    public Optional<string?> ExpansionComment { get; init; }
    public Optional<bool> ActionPlanNeeded { get; init; }
    public Optional<string> Highlights { get; init; }
}
