using PxOperations.Domain.Projects;

namespace PxOperations.Application.Features.ProjectHealth;

public sealed record ProjectHealthFilter(
    string? Search = null,
    DeliveryCenter? Dc = null,
    int? ProjectId = null,
    DateOnly? Week = null,
    int? MinScore = null,
    int? MaxScore = null);
