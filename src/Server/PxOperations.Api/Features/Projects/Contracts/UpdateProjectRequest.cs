using PxOperations.Domain.Abstractions;

namespace PxOperations.Api.Features.Projects.Contracts;

public sealed class UpdateProjectRequest
{
    public Optional<string> Dc { get; init; }
    public Optional<string> Status { get; init; }
    public Optional<string> Name { get; init; }
    public Optional<string?> Client { get; init; }
    public Optional<string> Type { get; init; }
    public Optional<string?> StartDate { get; init; }
    public Optional<string?> EndDate { get; init; }
    public Optional<string?> DeliveryManager { get; init; }
    public Optional<string> Renewal { get; init; }
    public Optional<string?> RenewalObservation { get; init; }
}
