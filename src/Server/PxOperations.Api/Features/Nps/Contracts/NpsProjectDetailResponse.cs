namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record NpsProjectDetailResponse(
    NpsProjectResponse Project,
    IReadOnlyList<NpsContactResponse> Contacts,
    IReadOnlyList<NpsDispatchResponse> Dispatches,
    IReadOnlyList<NpsSurveyResponse> RecentResponses);
