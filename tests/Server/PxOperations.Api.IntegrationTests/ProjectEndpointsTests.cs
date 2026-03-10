using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.Api.Features.Projects;
using PxOperations.Infrastructure.Persistence;

namespace PxOperations.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class ProjectEndpointsTests(PostgreSqlFixture fixture)
{
    private static readonly CreateProjectRequest ValidRequest = new(
        Dc: "DC1",
        Status: "Em andamento",
        Name: "Test Project",
        Client: "Test Client",
        Type: "Squad",
        StartDate: "2026-01-01",
        EndDate: "2026-12-31",
        DeliveryManager: "John Doe",
        Renewal: "None",
        RenewalObservation: null);

    [Fact]
    public async Task List_should_return_empty_after_cleanup()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanProjectsAsync(factory);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/projects");
        var projects = await response.Content.ReadFromJsonAsync<List<ProjectResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(projects);
        Assert.Empty(projects);
    }

    [Fact]
    public async Task Create_should_return_201()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/projects", ValidRequest);
        var project = await response.Content.ReadFromJsonAsync<ProjectResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(project);
        Assert.Equal("Test Project", project.Name);
        Assert.Equal("DC1", project.Dc);
        Assert.Equal("Em andamento", project.Status);
        Assert.Equal("Squad", project.Type);
        Assert.True(project.Id > 0);
    }

    [Fact]
    public async Task Create_should_return_400_when_name_is_empty()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var request = ValidRequest with { Name = "" };
        var response = await client.PostAsJsonAsync("/api/projects", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_should_return_project_after_create()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/projects", ValidRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();

        var response = await client.GetAsync($"/api/projects/{created!.Id}");
        var project = await response.Content.ReadFromJsonAsync<ProjectResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(project);
        Assert.Equal(created.Id, project.Id);
        Assert.Equal("Test Project", project.Name);
    }

    [Fact]
    public async Task GetById_should_return_404_for_nonexistent()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/projects/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_should_modify_only_sent_fields()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/projects", ValidRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();

        var patchRequest = new UpdateProjectRequest(Name: "Updated Name");
        var patchContent = JsonContent.Create(patchRequest);
        var response = await client.PatchAsync($"/api/projects/{created!.Id}", patchContent);
        var updated = await response.Content.ReadFromJsonAsync<ProjectResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("DC1", updated.Dc);
        Assert.Equal("Em andamento", updated.Status);
    }

    [Fact]
    public async Task Patch_should_return_404_for_nonexistent()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var patchRequest = new UpdateProjectRequest(Name: "Updated");
        var patchContent = JsonContent.Create(patchRequest);
        var response = await client.PatchAsync("/api/projects/99999", patchContent);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_should_return_204()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/projects", ValidRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();

        var response = await client.DeleteAsync($"/api/projects/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_should_return_404_for_nonexistent()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/projects/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_should_filter_by_dc()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanProjectsAsync(factory);
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/projects", ValidRequest);
        await client.PostAsJsonAsync("/api/projects", ValidRequest with { Dc = "DC2", Name = "Other Project" });

        var response = await client.GetAsync("/api/projects?dc=DC1");
        var projects = await response.Content.ReadFromJsonAsync<List<ProjectResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.All(projects, p => Assert.Equal("DC1", p.Dc));
    }

    private static async Task CleanProjectsAsync(ApiWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Projects.RemoveRange(dbContext.Projects);
        await dbContext.SaveChangesAsync();
    }
}
