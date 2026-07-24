namespace PxOperations.BlazorWasm.Features.Projects.Preview;

public sealed record ProjectsPreviewFilterState(
    string Search,
    IReadOnlyList<string> DeliveryCenters,
    IReadOnlyList<string> Statuses,
    IReadOnlyList<string> Types,
    IReadOnlyList<string> Renewals)
{
    public static ProjectsPreviewFilterState Empty { get; } = new(
        Search: string.Empty,
        DeliveryCenters: [],
        Statuses: [],
        Types: [],
        Renewals: []);
}
