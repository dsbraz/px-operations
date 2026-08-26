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
    bool IsOverdue,
    bool IsDismissed,
    string? DismissalReason,
    // B12: o quadro mostra o prazo do link aberto no card; sem isto não há como
    // trocar "expira em 3d" por "expirado há Xd".
    string? ActiveDispatchExpiresAt);
