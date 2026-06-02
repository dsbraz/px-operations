using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.Api.Features.ProjectHealth.Contracts;
using PxOperations.Api.Features.Projects.Contracts;
using PxOperations.Api.IntegrationTests.Infrastructure;
using PxOperations.Infrastructure.Persistence;

namespace PxOperations.Api.IntegrationTests.Features.ProjectHealth;

[Collection(ApiIntegrationCollection.Name)]
public sealed class ProjectHealthEndpointsTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task List_should_return_empty_after_cleanup()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/project-health");
        var entries = await response.Content.ReadFromJsonAsync<List<ProjectHealthResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(entries);
        Assert.Empty(entries);
    }

    [Fact]
    public async Task Create_should_return_201_with_computed_score()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Health");
        var response = await client.PostAsJsonAsync("/api/project-health", new CreateProjectHealthRequest(
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
            null,
            "Sprint entregue no prazo."));

        var ph = await response.Content.ReadFromJsonAsync<ProjectHealthResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(ph);
        Assert.Equal(projectId, ph.ProjectId);
        Assert.Equal("Projeto Health", ph.ProjectName);
        Assert.Equal("Squad Pagamentos", ph.SubProject);
        Assert.Equal(10, ph.Score);
        Assert.Equal("Verde", ph.Scope);
    }

    [Fact]
    public async Task Create_should_return_400_when_highlights_is_empty()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Validation");
        var response = await client.PostAsJsonAsync("/api/project-health", new CreateProjectHealthRequest(
            projectId, null, "2026-03-30", "joao@brq.com", 3,
            "verde", "verde", "verde", "verde",
            false, null, false, null, ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_should_return_400_when_project_does_not_exist()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/project-health", new CreateProjectHealthRequest(
            99999, null, "2026-03-30", "joao@brq.com", 3,
            "verde", "verde", "verde", "verde",
            false, null, false, null, "Teste."));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_should_return_400_when_scope_is_invalid()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Invalid RAG");
        var response = await client.PostAsJsonAsync("/api/project-health", new CreateProjectHealthRequest(
            projectId, null, "2026-03-30", "joao@brq.com", 3,
            "invalid", "verde", "verde", "verde",
            false, null, false, null, "Teste."));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_should_return_entry_after_create()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Get");
        var createResponse = await client.PostAsJsonAsync("/api/project-health", MakeRequest(projectId));
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectHealthResponse>();

        var getResponse = await client.GetAsync($"/api/project-health/{created!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<ProjectHealthResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
    }

    [Fact]
    public async Task GetById_should_return_404_for_nonexistent()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/project-health/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_should_modify_only_sent_fields()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Patch");
        var createResponse = await client.PostAsJsonAsync("/api/project-health", MakeRequest(projectId));
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectHealthResponse>();

        var patchResponse = await client.PatchAsync($"/api/project-health/{created!.Id}",
            new StringContent("""{"highlights": "Atualizado."}""", Encoding.UTF8, "application/json"));
        var updated = await patchResponse.Content.ReadFromJsonAsync<ProjectHealthResponse>();

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

        var response = await client.PatchAsync("/api/project-health/99999",
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
        var createResponse = await client.PostAsJsonAsync("/api/project-health", MakeRequest(projectId));
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectHealthResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/project-health/{created!.Id}");
        var getResponse = await client.GetAsync($"/api/project-health/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_should_return_404_for_nonexistent()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/project-health/99999");
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

        await client.PostAsJsonAsync("/api/project-health", MakeRequest(dc1));
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(dc2));

        var response = await client.GetAsync("/api/project-health?dc=DC1");
        var entries = await response.Content.ReadFromJsonAsync<List<ProjectHealthResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(entries);
        Assert.Single(entries);
        Assert.Equal("DC1", entries[0].ProjectDc);
    }

    [Fact]
    public async Task List_search_should_treat_percent_as_literal_not_wildcard()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var withPercent = await CreateProjectAsync(client, "Projeto 50% Cloud");
        var plain = await CreateProjectAsync(client, "Projeto Alfa");
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(withPercent));
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(plain));

        // "%" must match the literal character, not act as a LIKE wildcard (which would match everything).
        var response = await client.GetAsync("/api/project-health?search=%25");
        var entries = await response.Content.ReadFromJsonAsync<List<ProjectHealthResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(entries);
        Assert.Single(entries);
        Assert.Equal("Projeto 50% Cloud", entries[0].ProjectName);
    }

    [Fact]
    public async Task List_search_should_treat_underscore_as_literal_not_wildcard()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var withUnderscore = await CreateProjectAsync(client, "Projeto Squad_Backend");
        var without = await CreateProjectAsync(client, "Projeto SquadXBackend");
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(withUnderscore));
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(without));

        // "_" must match the literal character, not act as a single-char LIKE wildcard.
        var response = await client.GetAsync("/api/project-health?search=Squad_Backend");
        var entries = await response.Content.ReadFromJsonAsync<List<ProjectHealthResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(entries);
        Assert.Single(entries);
        Assert.Equal("Projeto Squad_Backend", entries[0].ProjectName);
    }

    [Fact]
    public async Task Summary_should_return_aggregated_data()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Summary");
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(projectId, scope: "verde", schedule: "verde", quality: "verde", satisfaction: "verde", practicesCount: 5));
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(projectId, scope: "vermelho", schedule: "vermelho", quality: "vermelho", satisfaction: "vermelho", practicesCount: 0, subProject: "Squad B"));

        var response = await client.GetAsync("/api/project-health/summary");
        var summary = await response.Content.ReadFromJsonAsync<ProjectHealthSummaryResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(summary);
        Assert.Equal(2, summary.TotalEntries);
        Assert.Equal(1, summary.TotalProjects);
        Assert.Equal(5, summary.AverageScore);
        Assert.Equal(5, summary.OverallAverageScore); // (10 + 0) / 2 over all active entries
        Assert.Equal(0, summary.NoResponseCount); // single project responded in the latest week
        Assert.Equal(1, summary.OverallCriticalCount); // the score-0 entry in the latest week
        Assert.Equal(0, summary.OverallNoResponseCount);
    }

    [Fact]
    public async Task Summary_should_count_active_project_without_health_in_total_and_no_response()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var withHealth = await CreateProjectAsync(client, "Com Registro");
        await CreateProjectAsync(client, "Sem Registro"); // active, no health entry
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(withHealth));

        var summary = await (await client.GetAsync("/api/project-health/summary"))
            .Content.ReadFromJsonAsync<ProjectHealthSummaryResponse>();

        Assert.NotNull(summary);
        Assert.Equal(2, summary.TotalProjects);
        Assert.Equal(1, summary.NoResponseCount);
    }

    [Fact]
    public async Task Summary_should_exclude_non_active_projects()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var active = await CreateProjectAsync(client, "Ativo");
        var closed = await CreateProjectAsync(client, "Encerrado", status: "Encerrado");
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(active));
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(closed));

        var summary = await (await client.GetAsync("/api/project-health/summary"))
            .Content.ReadFromJsonAsync<ProjectHealthSummaryResponse>();

        Assert.NotNull(summary);
        Assert.Equal(1, summary.TotalProjects);
        Assert.Equal(1, summary.TotalEntries); // closed project's entry excluded
        Assert.Equal(0, summary.NoResponseCount);
    }

    [Fact]
    public async Task Summary_metrics_should_reflect_only_latest_week()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Evolui");
        // Older week: critical (all red, no practices -> score 0)
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(projectId,
            scope: "vermelho", schedule: "vermelho", quality: "vermelho", satisfaction: "vermelho",
            practicesCount: 0, week: "2026-03-23"));
        // Latest week: healthy (all green, practices -> score 10)
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(projectId,
            scope: "verde", schedule: "verde", quality: "verde", satisfaction: "verde",
            practicesCount: 5, week: "2026-03-30"));

        var summary = await (await client.GetAsync("/api/project-health/summary"))
            .Content.ReadFromJsonAsync<ProjectHealthSummaryResponse>();

        Assert.NotNull(summary);
        Assert.Equal(1, summary.TotalProjects);
        Assert.Equal(1, summary.TotalEntries);   // counters use the latest week only
        Assert.Equal(0, summary.CriticalCount);  // old critical entry not counted
        Assert.Equal(1, summary.HealthyCount);
        // No period selected: "Saúde Geral" defaults to the overall average of all active entries...
        Assert.Equal(5.0, summary.AverageScore);          // (0 + 10) / 2
        Assert.Equal(5.0, summary.OverallAverageScore);   // ...and the top "Média" is always the overall
    }

    [Fact]
    public async Task Summary_should_be_invariant_to_score_filter()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Critico");
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(projectId,
            scope: "vermelho", schedule: "vermelho", quality: "vermelho", satisfaction: "vermelho",
            practicesCount: 0));

        var noFilter = await (await client.GetAsync("/api/project-health/summary"))
            .Content.ReadFromJsonAsync<ProjectHealthSummaryResponse>();
        var healthyBand = await (await client.GetAsync("/api/project-health/summary?minScore=7&maxScore=10"))
            .Content.ReadFromJsonAsync<ProjectHealthSummaryResponse>();

        Assert.NotNull(noFilter);
        Assert.NotNull(healthyBand);
        Assert.Equal(1, noFilter.TotalProjects);
        Assert.Equal(0, noFilter.NoResponseCount);
        Assert.Equal(1, noFilter.CriticalCount);
        // Score filter must not shrink the carteira nor change the snapshot aggregates
        Assert.Equal(noFilter.TotalProjects, healthyBand.TotalProjects);
        Assert.Equal(noFilter.NoResponseCount, healthyBand.NoResponseCount);
        Assert.Equal(noFilter.CriticalCount, healthyBand.CriticalCount);
        Assert.Equal(noFilter.AverageScore, healthyBand.AverageScore);
        Assert.Equal(noFilter.OverallAverageScore, healthyBand.OverallAverageScore);
    }

    [Fact]
    public async Task Summary_should_honor_explicit_week()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var projectId = await CreateProjectAsync(client, "Projeto Periodo");
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(projectId,
            scope: "vermelho", schedule: "vermelho", quality: "vermelho", satisfaction: "vermelho",
            practicesCount: 0, week: "2026-03-23"));
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(projectId,
            scope: "verde", schedule: "verde", quality: "verde", satisfaction: "verde",
            practicesCount: 5, week: "2026-03-30"));

        var oldWeek = await (await client.GetAsync("/api/project-health/summary?week=2026-03-23"))
            .Content.ReadFromJsonAsync<ProjectHealthSummaryResponse>();
        var latest = await (await client.GetAsync("/api/project-health/summary"))
            .Content.ReadFromJsonAsync<ProjectHealthSummaryResponse>();

        Assert.NotNull(oldWeek);
        Assert.NotNull(latest);
        Assert.Equal(1, oldWeek.CriticalCount);          // KPI counter follows the selected week
        Assert.Equal(0, oldWeek.OverallCriticalCount);   // top "Críticos" stays fixed on the latest week (healthy)
        Assert.Equal(0, oldWeek.OverallNoResponseCount); // top "Sem resp" stays fixed on the latest week
        Assert.Equal(0, oldWeek.HealthyCount);
        Assert.Equal(0.0, oldWeek.AverageScore);         // week filter -> that week's snapshot (critical)
        Assert.Equal(5.0, oldWeek.OverallAverageScore);  // top "Média" is always the overall (0+10)/2
        Assert.Equal(0, latest.CriticalCount);
        Assert.Equal(1, latest.HealthyCount);
        Assert.Equal(5.0, latest.AverageScore);          // no period -> "Saúde Geral" defaults to overall
        Assert.Equal(5.0, latest.OverallAverageScore);
    }

    [Fact]
    public async Task Summary_should_scope_total_projects_by_dc()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var dc1 = await CreateProjectAsync(client, "Projeto DC1", dc: "DC1");
        var dc2 = await CreateProjectAsync(client, "Projeto DC2", dc: "DC2");
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(dc1));
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(dc2));

        var all = await (await client.GetAsync("/api/project-health/summary"))
            .Content.ReadFromJsonAsync<ProjectHealthSummaryResponse>();
        var onlyDc1 = await (await client.GetAsync("/api/project-health/summary?dc=DC1"))
            .Content.ReadFromJsonAsync<ProjectHealthSummaryResponse>();

        Assert.NotNull(all);
        Assert.NotNull(onlyDc1);
        Assert.Equal(2, all.TotalProjects);
        Assert.Equal(1, onlyDc1.TotalProjects);
        Assert.Equal(1, onlyDc1.TotalEntries);    // only DC1's entry in scope
        Assert.Equal(0, onlyDc1.NoResponseCount);
    }

    [Fact]
    public async Task Summary_should_scope_aggregates_by_project()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        var target = await CreateProjectAsync(client, "Projeto Alvo");
        var other = await CreateProjectAsync(client, "Projeto Outro");
        // Target scores 10 (all green); the other scores 0 (all red). A per-project summary must ignore the other.
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(target));
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(other,
            scope: "vermelho", schedule: "vermelho", quality: "vermelho", satisfaction: "vermelho", practicesCount: 0));

        var all = await (await client.GetAsync("/api/project-health/summary"))
            .Content.ReadFromJsonAsync<ProjectHealthSummaryResponse>();
        var onlyTarget = await (await client.GetAsync($"/api/project-health/summary?projectId={target}"))
            .Content.ReadFromJsonAsync<ProjectHealthSummaryResponse>();

        Assert.NotNull(all);
        Assert.NotNull(onlyTarget);
        Assert.Equal(2, all.TotalProjects);
        Assert.Equal(5, all.OverallAverageScore);     // (10 + 0) / 2 across both active projects
        Assert.Equal(1, onlyTarget.TotalProjects);    // scoped to the requested project only
        Assert.Equal(1, onlyTarget.TotalEntries);     // only the target's entry in scope
        Assert.Equal(10, onlyTarget.OverallAverageScore);
        Assert.Equal(0, onlyTarget.NoResponseCount);
    }

    [Fact]
    public async Task Summary_expansion_count_should_reflect_only_latest_week()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        await CleanAsync(factory);
        using var client = factory.CreateClient();

        // Project with an expansion opportunity in the latest week -> counts.
        var current = await CreateProjectAsync(client, "Expansao Atual");
        await client.PostAsJsonAsync("/api/project-health", new CreateProjectHealthRequest(
            current, null, "2026-03-30", "joao@brq.com", 3, "verde", "verde", "verde", "verde",
            true, "Oportunidade atual", false, null, "Crescendo."));

        // Project whose expansion was only in an older week; latest week has none -> does not count.
        var stale = await CreateProjectAsync(client, "Expansao Antiga");
        await client.PostAsJsonAsync("/api/project-health", new CreateProjectHealthRequest(
            stale, null, "2026-03-23", "joao@brq.com", 3, "verde", "verde", "verde", "verde",
            true, "Oportunidade antiga", false, null, "Tinha chance."));
        await client.PostAsJsonAsync("/api/project-health", MakeRequest(stale, week: "2026-03-30"));

        var summary = await (await client.GetAsync("/api/project-health/summary"))
            .Content.ReadFromJsonAsync<ProjectHealthSummaryResponse>();

        Assert.NotNull(summary);
        Assert.Equal(2, summary.TotalProjects);
        Assert.Equal(1, summary.WithExpansionCount); // only the latest-week opportunity
    }

    private static CreateProjectHealthRequest MakeRequest(
        int projectId,
        string? subProject = null,
        string scope = "verde",
        string schedule = "verde",
        string quality = "verde",
        string satisfaction = "verde",
        int practicesCount = 3,
        string highlights = "Tudo certo nesta semana.",
        string week = "2026-03-30")
    {
        return new CreateProjectHealthRequest(
            projectId, subProject, week, "joao@brq.com", practicesCount,
            scope, schedule, quality, satisfaction,
            false, null, false, null, highlights);
    }

    private static async Task<int> CreateProjectAsync(HttpClient client, string name, string dc = "DC1", string status = "Em andamento")
    {
        var response = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest(
            Dc: dc,
            Status: status,
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
        dbContext.ProjectHealth.RemoveRange(dbContext.ProjectHealth);
        dbContext.Milestones.RemoveRange(dbContext.Milestones);
        dbContext.Projects.RemoveRange(dbContext.Projects);
        await dbContext.SaveChangesAsync();
    }
}
