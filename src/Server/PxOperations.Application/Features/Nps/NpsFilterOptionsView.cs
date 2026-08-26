namespace PxOperations.Application.Features.Nps;

/// <summary>
/// F1 oferece empresa e DM como facetas, e ambas são texto livre — o menu
/// precisa dos valores que existem. Derivar da lista já carregada não serve:
/// ela vem filtrada, então as opções encolheriam conforme o usuário filtra.
/// </summary>
public sealed record NpsFilterOptionsView(
    IReadOnlyList<string> Companies,
    IReadOnlyList<string> DeliveryManagers);
