using PxOperations.Domain.Projects;

namespace PxOperations.Application.Features.HealthChecks;

public sealed record HealthCheckFilter(
    string? Search = null,
    DeliveryCenter? Dc = null,
    int? ProjectId = null,
    DateOnly? Week = null,
    int? MinScore = null,
    int? MaxScore = null);
