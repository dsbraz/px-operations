using PxOperations.Domain.Nps;

namespace PxOperations.Application.Features.Nps;

public sealed record NpsFilter(
    string? Search,
    string? Dc,
    string? DeliveryManager,
    string? ProjectType,
    int? ProjectId,
    DateOnly? From,
    DateOnly? To,
    NpsClassification? Classification);
