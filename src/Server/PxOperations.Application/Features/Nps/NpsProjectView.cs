namespace PxOperations.Application.Features.Nps;

public sealed record NpsProjectView(
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
