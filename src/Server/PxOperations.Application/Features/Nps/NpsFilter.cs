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
    NpsClassification? Classification,
    // F1: "coletas dispensadas" é toggle Ocultar/Mostrar, não faceta de lista.
    // Padrão oculto: o quadro existe para mostrar o que precisa de ação.
    bool IncludeDismissed = false)
{
    public static NpsFilter None => new(null, null, null, null, null, null, null, null);

    public static NpsFilter ForProject(int projectId)
        => new(null, null, null, null, projectId, null, null, null);
}
