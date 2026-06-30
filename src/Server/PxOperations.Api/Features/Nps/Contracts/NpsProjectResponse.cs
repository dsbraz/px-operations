namespace PxOperations.Api.Features.Nps.Contracts;

public sealed record NpsProjectResponse(
    int Id,
    string Name,
    string? Client,
    string Dc,
    string? DeliveryManager,
    int ContactsCount,
    int ActiveDispatches,
    int LinkTargetsCount,
    int AnsweredLinkTargetsCount,
    int ResponsesCount,
    string? LastResponseAt,
    decimal? LastNps,
    bool IsOverdue);
