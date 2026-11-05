namespace PxOperations.Domain.Team;

public static class TeamRoster
{
    public static IReadOnlyList<TeamMember> Members { get; } =
    [
        new("Julia Pereira", TeamArea.Sales),
        new("Daniel Jesus", TeamArea.Sales),
        new("Matheus Cunha", TeamArea.Sales),
        new("Filipe Bertoncini", TeamArea.TaaS),
        new("Renata Fabri", TeamArea.TaaS),
        new("Denise Santos", TeamArea.Operations),
        new("Natalia Assis", TeamArea.Operations),
        new("Igor Magrini", TeamArea.Operations),
        new("Pablo Moura", TeamArea.Operations),
        new("Carla Paixão", TeamArea.Projects),
        new("Pedro Benites", TeamArea.Projects),
        new("Pedro Vieira", TeamArea.Projects),
        new("Jefferson Ramirez", TeamArea.Projects),
        new("Carol Fabrizio", TeamArea.Projects)
    ];

    public static bool IsMember(string name) => Members.Any(member => member.Name == name);
}
