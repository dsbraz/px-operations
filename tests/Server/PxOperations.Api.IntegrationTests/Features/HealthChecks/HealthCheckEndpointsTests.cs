using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.Api.Features.HealthChecks.Contracts;
using PxOperations.Api.Features.Projects.Contracts;
using PxOperations.Api.IntegrationTests.Infrastructure;
using PxOperations.Infrastructure.Persistence;

namespace PxOperations.Api.IntegrationTests.Features.HealthChecks;

[Collection(ApiIntegrationCollection.Name)]
public sealed class HealthCheckEndpointsTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task List_should_return_empty_after_cleanup()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health-checks");
        var healthChecks = await response.Content.ReadFromJsonAsync<List<HealthCheckResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(healthChecks);
        Assert.Empty(healthChecks);
    }

    [Fact]
    public async Task Create_should_return_201_with_computed_score()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Health");
        var response = await client.PostAsJsonAsync("/api/health-checks", new CreateHealthCheckRequest(
            projectId,
            "Squad Pagamentos",
            "2026-03-30",
            "joao@brq.com",
            5,
            "verde",
            "verde",
            "verde",
            "verde",
            false,
            null,
            false,
            "Sprint entregue no prazo."));

        var hc = await response.Content.ReadFromJsonAsync<HealthCheckResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(hc);
        Assert.Equal(projectId, hc.ProjectId);
        Assert.Equal("Projeto Health", hc.ProjectName);
        Assert.Equal("Squad Pagamentos", hc.SubProject);
        Assert.Equal(10, hc.Score);
        Assert.Equal("Verde", hc.Scope);
    }

    [Fact]
    public async Task Create_should_return_400_when_highlights_is_empty()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Validation");
        var response = await client.PostAsJsonAsync("/api/health-checks", new CreateHealthCheckRequest(
            projectId, null, "2026-03-30", "joao@brq.com", 3,
            "verde", "verde", "verde", "verde",
            false, null, false, ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_should_return_400_when_project_does_not_exist()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/health-checks", new CreateHealthCheckRequest(
            99999, null, "2026-03-30", "joao@brq.com", 3,
            "verde", "verde", "verde", "verde",
            false, null, false, "Teste."));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_should_return_400_when_scope_is_invalid()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Invalid RAG");
        var response = await client.PostAsJsonAsync("/api/health-checks", new CreateHealthCheckRequest(
            projectId, null, "2026-03-30", "joao@brq.com", 3,
            "invalid", "verde", "verde", "verde",
            false, null, false, "Teste."));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_should_return_entry_after_create()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Get");
        var createResponse = await client.PostAsJsonAsync("/api/health-checks", MakeRequest(projectId));
        var created = await createResponse.Content.ReadFromJsonAsync<HealthCheckResponse>();

        var getResponse = await client.GetAsync($"/api/health-checks/{created!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<HealthCheckResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
    }

    [Fact]
    public async Task GetById_should_return_404_for_nonexistent()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health-checks/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_should_modify_only_sent_fields()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Patch");
        var createResponse = await client.PostAsJsonAsync("/api/health-checks", MakeRequest(projectId));
        var created = await createResponse.Content.ReadFromJsonAsync<HealthCheckResponse>();

        var patchResponse = await client.PatchAsync($"/api/health-checks/{created!.Id}",
            new StringContent("""{"highlights": "Atualizado."}""", Encoding.UTF8, "application/json"));
        var updated = await patchResponse.Content.ReadFromJsonAsync<HealthCheckResponse>();

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Atualizado.", updated.Highlights);
        Assert.Equal(created.Score, updated.Score);
    }

    [Fact]
    public async Task Patch_should_return_404_for_nonexistent()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.PatchAsync("/api/health-checks/99999",
            new StringContent("""{"highlights": "x"}""", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_should_return_204()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Delete");
        var createResponse = await client.PostAsJsonAsync("/api/health-checks", MakeRequest(projectId));
        var created = await createResponse.Content.ReadFromJsonAsync<HealthCheckResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/health-checks/{created!.Id}");
        var getResponse = await client.GetAsync($"/api/health-checks/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_should_return_404_for_nonexistent()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/health-checks/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_should_filter_by_dc()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var dc1 = await CreateProjectAsync(client, "Projeto DC1", dc: "DC1");
        var dc2 = await CreateProjectAsync(client, "Projeto DC2", dc: "DC2");

        await client.PostAsJsonAsync("/api/health-checks", MakeRequest(dc1));
        await client.PostAsJsonAsync("/api/health-checks", MakeRequest(dc2));

        var response = await client.GetAsync("/api/health-checks?dc=DC1");
        var healthChecks = await response.Content.ReadFromJsonAsync<List<HealthCheckResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(healthChecks);
        Assert.Single(healthChecks);
        Assert.Equal("DC1", healthChecks[0].ProjectDc);
    }

    [Fact]
    public async Task Summary_should_return_aggregated_data()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Summary");
        await client.PostAsJsonAsync("/api/health-checks", MakeRequest(projectId, scope: "verde", schedule: "verde", quality: "verde", satisfaction: "verde", practicesCount: 5));
        await client.PostAsJsonAsync("/api/health-checks", MakeRequest(projectId, scope: "vermelho", schedule: "vermelho", quality: "vermelho", satisfaction: "vermelho", practicesCount: 0, subProject: "Squad B"));

        var response = await client.GetAsync("/api/health-checks/summary");
        var summary = await response.Content.ReadFromJsonAsync<HealthCheckSummaryResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(summary);
        Assert.Equal(2, summary.TotalEntries);
        Assert.Equal(1, summary.TotalProjects);
        Assert.Equal(5, summary.AverageScore);
    }

    private static CreateHealthCheckRequest MakeRequest(
        int projectId,
        string? subProject = null,
        string scope = "verde",
        string schedule = "verde",
        string quality = "verde",
        string satisfaction = "verde",
        int practicesCount = 3,
        string highlights = "Tudo certo nesta semana.")
    {
        return new CreateHealthCheckRequest(
            projectId, subProject, "2026-03-30", "joao@brq.com", practicesCount,
            scope, schedule, quality, satisfaction,
            false, null, false, highlights);
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
        dbContext.HealthChecks.RemoveRange(dbContext.HealthChecks);
        dbContext.Milestones.RemoveRange(dbContext.Milestones);
        dbContext.Projects.RemoveRange(dbContext.Projects);
        await dbContext.SaveChangesAsync();
    }
}
