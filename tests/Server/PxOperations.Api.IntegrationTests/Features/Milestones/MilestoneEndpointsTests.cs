using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.Api.Features.Milestones.Contracts;
using PxOperations.Api.Features.Projects.Contracts;
using PxOperations.Api.IntegrationTests.Infrastructure;
using PxOperations.Infrastructure.Persistence;

namespace PxOperations.Api.IntegrationTests.Features.Milestones;

[Collection(ApiIntegrationCollection.Name)]
public sealed class MilestoneEndpointsTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Create_should_return_201_and_enriched_response()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Marco");
        var response = await client.PostAsJsonAsync("/api/milestones", new CreateMilestoneRequest(
            projectId,
            "Kickoff",
            "Kickoff oficial",
            "2026-03-20",
            "09:30",
            "Sala 1"));

        var milestone = await response.Content.ReadFromJsonAsync<MilestoneResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(milestone);
        Assert.Equal(projectId, milestone.ProjectId);
        Assert.Equal("Projeto Marco", milestone.ProjectName);
        Assert.Equal("Kickoff", milestone.Type);
    }

    [Fact]
    public async Task List_should_filter_by_project_dc_and_date_range()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var dc1Project = await CreateProjectAsync(client, "Projeto DC1", dc: "DC1");
        var dc2Project = await CreateProjectAsync(client, "Projeto DC2", dc: "DC2");

        await client.PostAsJsonAsync("/api/milestones", new CreateMilestoneRequest(dc1Project, "Kickoff", "Marco A", "2026-03-20", null, null));
        await client.PostAsJsonAsync("/api/milestones", new CreateMilestoneRequest(dc2Project, "Outros", "Marco B", "2026-04-20", null, null));

        var response = await client.GetAsync("/api/milestones?dc=DC1&from=2026-03-01&to=2026-03-31");
        var milestones = await response.Content.ReadFromJsonAsync<List<MilestoneResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(milestones);
        Assert.Single(milestones);
        Assert.Equal("Projeto DC1", milestones[0].ProjectName);
    }

    [Fact]
    public async Task Patch_should_update_existing_milestone()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Alteracao");
        var createResponse = await client.PostAsJsonAsync("/api/milestones", new CreateMilestoneRequest(projectId, "Outros", "Original", "2026-03-20", null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<MilestoneResponse>();

        var patchResponse = await client.PatchAsync($"/api/milestones/{created!.Id}",
            new StringContent("""{"title": "Atualizado", "type": "Entrega Final"}""", Encoding.UTF8, "application/json"));
        var updated = await patchResponse.Content.ReadFromJsonAsync<MilestoneResponse>();

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Atualizado", updated.Title);
        Assert.Equal("Entrega Final", updated.Type);
    }

    [Fact]
    public async Task Delete_should_remove_milestone()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Exclusao");
        var createResponse = await client.PostAsJsonAsync("/api/milestones", new CreateMilestoneRequest(projectId, "Outros", "Excluir", "2026-03-20", null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<MilestoneResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/milestones/{created!.Id}");
        var getResponse = await client.GetAsync($"/api/milestones/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_project_should_cascade_delete_milestones()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Cascade");
        await client.PostAsJsonAsync("/api/milestones", new CreateMilestoneRequest(projectId, "Outros", "Marco dependente", "2026-03-20", null, null));

        var deleteProjectResponse = await client.DeleteAsync($"/api/projects/{projectId}");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(HttpStatusCode.NoContent, deleteProjectResponse.StatusCode);
        Assert.Empty(dbContext.Milestones);
    }

    private static async Task<int> CreateProjectAsync(HttpClient client, string name, string dc = "DC1")
    {
        var response = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest(
            Dc: dc,
            Status: "Em andamento",
            Name: name,
            Client: "Cliente X",
            Type: "Squad",
            StartDate: "2026-01-01",
            EndDate: "2026-12-31",
            DeliveryManager: "DM X",
            Renewal: "None",
            RenewalObservation: null));

        var project = await response.Content.ReadFromJsonAsync<ProjectResponse>();
        return project!.Id;
    }

    private static async Task CleanAsync(ApiWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Milestones.RemoveRange(dbContext.Milestones);
        dbContext.Projects.RemoveRange(dbContext.Projects);
        await dbContext.SaveChangesAsync();
    }
}
