using PxOperations.Domain.Team;

namespace PxOperations.Domain.UnitTests.Team;

public sealed class TeamRosterTests
{
    [Fact]
    public void Roster_should_list_the_governance_areas_in_order()
    {
        var groups = TeamRoster.Members
            .GroupBy(member => member.Area)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(member => member.Name))}");

        Assert.Equal(
            new[]
            {
                "Sales: Julia Pereira, Daniel Jesus, Matheus Cunha",
                "TaaS: Filipe Bertoncini, Renata Fabri",
                "Operations: Denise Santos, Natalia Assis, Igor Magrini, Pablo Moura",
                "Projects: Carla Paixão, Pedro Benites, Pedro Vieira, Jefferson Ramirez, Carol Fabrizio"
            },
            groups);
    }

    [Theory]
    [InlineData(TeamArea.Sales, false)]
    [InlineData(TeamArea.TaaS, true)]
    [InlineData(TeamArea.Operations, true)]
    [InlineData(TeamArea.Projects, true)]
    public void Only_sales_should_not_be_responsible_for_projects(TeamArea area, bool expected)
    {
        Assert.Equal(expected, new TeamMember("Pessoa", area).CanBeDeliveryManager);
    }

    [Theory]
    [InlineData("Julia Pereira", true)]
    [InlineData("Pedro Vieira", true)]
    [InlineData("Flavia de Castro", false)]
    [InlineData("julia pereira", false)]
    [InlineData("", false)]
    public void Membership_should_match_the_exact_name(string name, bool expected)
    {
        Assert.Equal(expected, TeamRoster.IsMember(name));
    }
}
