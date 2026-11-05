using System.Net.Http.Json;
using PxOperations.Api.IntegrationTests.Infrastructure;
using PxOperations.Application.Features.Team;

namespace PxOperations.Api.IntegrationTests.Features.Team;

[Collection(ApiIntegrationCollection.Name)]
public sealed class TeamMembersEndpointsTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Team_members_should_say_who_can_be_responsible_for_a_project()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var members = await client.GetFromJsonAsync<List<TeamMemberView>>("/api/team-members");

        Assert.Equal(14, members!.Count);
        Assert.Equal(new TeamMemberView("Julia Pereira", false), members[0]);
        Assert.Equal(new TeamMemberView("Filipe Bertoncini", true), members[3]);
        Assert.Equal(new TeamMemberView("Denise Santos", true), members[5]);
        Assert.Equal(new TeamMemberView("Carol Fabrizio", true), members[13]);
        Assert.Equal(3, members.Count(member => !member.CanBeDeliveryManager));
    }
}
