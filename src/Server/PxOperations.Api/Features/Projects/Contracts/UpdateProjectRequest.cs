namespace PxOperations.Api.Features.Projects.Contracts;

public sealed record UpdateProjectRequest(
    string? Dc = null,
    string? Status = null,
    string? Name = null,
    string? Client = null,
    string? Type = null,
    string? StartDate = null,
    string? EndDate = null,
    string? DeliveryManager = null,
    string? Renewal = null,
    string? RenewalObservation = null);
