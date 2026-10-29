using System.Net.Http.Json;
using PxOperations.Api.IntegrationTests.Infrastructure;
using PxOperations.Application.Features.Team;

namespace PxOperations.Api.IntegrationTests.Features.Team;

[Collection(ApiIntegrationCollection.Name)]
public sealed class TeamMembersEndpointsTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Team_members_should_carry_the_area_and_who_can_be_responsible_for_a_project()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var members = await client.GetFromJsonAsync<List<TeamMemberView>>("/api/team-members");

        Assert.Equal(14, members!.Count);
        Assert.Equal(new TeamMemberView("Julia Pereira", "sales", "Sales", false), members[0]);
        Assert.Equal(new TeamMemberView("Filipe Bertoncini", "taas", "TaaS", true), members[3]);
        Assert.Equal(new TeamMemberView("Denise Santos", "operations", "Operações", true), members[5]);
        Assert.Equal(new TeamMemberView("Carol Fabrizio", "projects", "Projetos", true), members[13]);
        Assert.Equal(3, members.Count(member => !member.CanBeDeliveryManager));
    }
}
