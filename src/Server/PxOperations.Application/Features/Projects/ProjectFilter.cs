using PxOperations.Domain.Projects;

namespace PxOperations.Application.Features.Projects;

public sealed record ProjectFilter(
    string? Search = null,
    DeliveryCenter? Dc = null,
    ProjectStatus? Status = null,
    ProjectType? Type = null,
    RenewalStatus? Renewal = null);
