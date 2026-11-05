namespace PxOperations.Domain.Team;

public sealed record TeamMember(string Name, TeamArea Area)
{
    // Todos geram NPS, mas Sales não responde por projeto: o campo que a tela
    // chama de "Responsável" ainda é o DeliveryManager do projeto.
    public bool CanBeDeliveryManager => Area != TeamArea.Sales;
}
