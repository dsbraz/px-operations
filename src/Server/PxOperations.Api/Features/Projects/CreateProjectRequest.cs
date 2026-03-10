namespace PxOperations.Api.Features.Projects;

public sealed record CreateProjectRequest(
    string Dc,
    string Status,
    string Name,
    string? Client,
    string Type,
    string? StartDate,
    string? EndDate,
    string? DeliveryManager,
    string? Renewal,
    string? RenewalObservation);
