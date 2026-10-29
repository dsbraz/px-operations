using PxOperations.Domain.Team;

namespace PxOperations.Application.Features.Team;

public sealed record TeamMemberView(string Name, string Area, string AreaLabel, bool CanBeDeliveryManager);

public static class TeamMemberViews
{
    public static IReadOnlyList<TeamMemberView> List()
        => TeamRoster.Members
            .Select(member => new TeamMemberView(
                member.Name,
                AreaCode(member.Area),
                AreaLabel(member.Area),
                member.CanBeDeliveryManager))
            .ToArray();

    private static string AreaCode(TeamArea area) => area switch
    {
        TeamArea.Sales => "sales",
        TeamArea.TaaS => "taas",
        TeamArea.Operations => "operations",
        _ => "projects"
    };

    private static string AreaLabel(TeamArea area) => area switch
    {
        TeamArea.Sales => "Sales",
        TeamArea.TaaS => "TaaS",
        TeamArea.Operations => "Operações",
        _ => "Projetos"
    };
}
