namespace PxOperations.Application.Features.Nps;

public sealed record NpsFilter(
    string? Search,
    IReadOnlyList<string> Clients,
    IReadOnlyList<string> Dcs,
    IReadOnlyList<string> ProjectTypes,
    IReadOnlyList<string> DeliveryManagers,
    IReadOnlyList<string> Statuses,
    IReadOnlyList<string> Formats,
    IReadOnlyList<string> Classifications,
    DateOnly? From,
    DateOnly? To,
    bool IncludeWaived,
    int? ProjectId)
{
    public static NpsFilter Empty { get; } = new(
        null, [], [], [], [], [], [], [], null, null, false, null);
}
