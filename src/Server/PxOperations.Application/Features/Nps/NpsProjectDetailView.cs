namespace PxOperations.Application.Features.Nps;

public sealed record NpsProjectDetailView(
    NpsProjectView Project,
    IReadOnlyList<NpsContactView> Contacts,
    IReadOnlyList<NpsDispatchView> Dispatches,
    IReadOnlyList<NpsResponseView> RecentResponses);
