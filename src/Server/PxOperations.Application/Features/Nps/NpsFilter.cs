using PxOperations.Domain.Nps;
using PxOperations.Domain.Projects;

namespace PxOperations.Application.Features.Nps;

/// <summary>
/// D11: as facetas de LISTA aceitam vários valores e filtram pela união deles;
/// entre facetas diferentes vale a interseção. Período é intervalo, então
/// segue single, e "coletas dispensadas" é toggle, não faceta.
/// </summary>
public sealed record NpsFilter(
    string? Search,
    IReadOnlyList<string>? Companies,
    IReadOnlyList<DeliveryCenter>? Dcs,
    IReadOnlyList<string>? DeliveryManagers,
    IReadOnlyList<ProjectType>? ProjectTypes,
    IReadOnlyList<NpsCollectionStatus>? CollectionStatuses,
    int? ProjectId,
    DateOnly? From,
    DateOnly? To,
    IReadOnlyList<NpsClassification>? Classifications,
    // Faceta de RESPOSTA, como a classificação: o formato vem do disparo que
    // originou cada resposta, não do projeto.
    IReadOnlyList<NpsFormFormat>? Formats,
    // F1: "coletas dispensadas" é toggle Ocultar/Mostrar, não faceta de lista.
    // Padrão oculto: o quadro existe para mostrar o que precisa de ação.
    bool IncludeDismissed = false)
{
    public static NpsFilter None => new(null, null, null, null, null, null, null, null, null, null, null);

    public static NpsFilter ForProject(int projectId)
        => new(null, null, null, null, null, null, projectId, null, null, null, null);
}
