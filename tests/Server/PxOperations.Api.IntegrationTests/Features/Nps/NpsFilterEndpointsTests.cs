using System.Net;
using System.Net.Http.Json;
using PxOperations.Api.Features.Nps.Contracts;
using PxOperations.Api.Features.Projects.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.Api.IntegrationTests.Infrastructure;
using PxOperations.Infrastructure.Persistence;

namespace PxOperations.Api.IntegrationTests.Features.Nps;

/// <summary>
/// B15/D11: cada faceta de lista aceita vários valores. O critério de aceite
/// de F1 é literal — "marcar dois valores da mesma faceta filtra pela união
/// deles" — e entre facetas diferentes vale a interseção.
///
/// Os testes compartilham o banco, então cada um cria projetos com um marcador
/// próprio e passa esse marcador em `search`: sem isso a asserção de contagem
/// enxergaria os projetos dos outros testes.
/// </summary>
[Collection(ApiIntegrationCollection.Name)]
public sealed class NpsFilterEndpointsTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ListProjects_should_return_the_union_of_two_values_of_the_same_facet()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Uniao{Guid.NewGuid():N}";

        await CreateProjectAsync(client, marker, dc: "DC1");
        await CreateProjectAsync(client, marker, dc: "DC2");
        await CreateProjectAsync(client, marker, dc: "DC3");

        var projects = await ListAsync(client, $"search={marker}&dc=DC1&dc=DC2");

        Assert.Equal(2, projects.Count);
        Assert.All(projects, p => Assert.Contains(p.Dc, new[] { "DC1", "DC2" }));
    }

    [Fact]
    public async Task ListProjects_should_intersect_values_of_different_facets()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Intersecao{Guid.NewGuid():N}";

        var squadDc1 = await CreateProjectAsync(client, marker, dc: "DC1", type: "Squad");
        var squadDc2 = await CreateProjectAsync(client, marker, dc: "DC2", type: "Squad");
        await CreateProjectAsync(client, marker, dc: "DC1", type: "Alocação");

        var projects = await ListAsync(client, $"search={marker}&dc=DC1&dc=DC2&projectType=Squad");

        Assert.Equal(2, projects.Count);
        Assert.Equal([squadDc1.Id, squadDc2.Id], projects.Select(p => p.Id).Order().ToList());
    }

    [Fact]
    public async Task ListProjects_should_filter_by_company()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Empresa{Guid.NewGuid():N}";

        await CreateProjectAsync(client, marker, company: "Santander");
        await CreateProjectAsync(client, marker, company: "Itaú");
        await CreateProjectAsync(client, marker, company: "Bradesco");

        var projects = await ListAsync(client, $"search={marker}&company=Santander&company=Ita%C3%BA");

        Assert.Equal(2, projects.Count);
        Assert.All(projects, p => Assert.Contains(p.Client, new[] { "Santander", "Itaú" }));
    }

    [Fact]
    public async Task ListProjects_should_filter_by_delivery_manager_by_exact_value()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Dm{Guid.NewGuid():N}";

        await CreateProjectAsync(client, marker, deliveryManager: "Pedro Vieira");
        await CreateProjectAsync(client, marker, deliveryManager: "Flavia de Castro");

        var projects = await ListAsync(client, $"search={marker}&deliveryManager=Pedro%20Vieira");

        Assert.Single(projects);
        Assert.Equal("Pedro Vieira", projects[0].DeliveryManager);
    }

    [Fact]
    public async Task ListProjects_should_filter_by_collection_status()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Status{Guid.NewGuid():N}";

        var withLink = await CreateProjectAsync(client, marker);
        await CreateProjectAsync(client, marker);
        await CreateDispatchAsync(client, withLink.Id);

        var linkSent = await ListAsync(client, $"search={marker}&status=Link%20gerado");
        var pending = await ListAsync(client, $"search={marker}&status=Pendente");

        Assert.Single(linkSent);
        Assert.Equal(withLink.Id, linkSent[0].Id);
        Assert.Equal("Link gerado", linkSent[0].CollectionStatus);
        Assert.Single(pending);
        Assert.Equal("Pendente", pending[0].CollectionStatus);
    }

    /// <summary>
    /// F2: a idade do card "Sem link" sai do fechamento do último disparo. É o
    /// único caso em que existe data real — projeto que nunca teve link fica
    /// sem o campo, e o quadro omite o chip em vez de inventar a conta.
    /// </summary>
    [Fact]
    public async Task ListProjects_should_expose_when_the_last_link_was_closed()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Fechado{Guid.NewGuid():N}";

        var withClosedLink = await CreateProjectAsync(client, marker);
        var neverHadLink = await CreateProjectAsync(client, marker);
        var dispatch = await CreateDispatchAsync(client, withClosedLink.Id);
        (await client.PatchAsync($"/api/nps/dispatches/{dispatch}/close", null)).EnsureSuccessStatusCode();

        var projects = await ListAsync(client, $"search={marker}");

        Assert.NotNull(projects.Single(p => p.Id == withClosedLink.Id).LastDispatchClosedAt);
        Assert.Null(projects.Single(p => p.Id == neverHadLink.Id).LastDispatchClosedAt);
    }

    [Fact]
    public async Task ListProjects_should_reject_a_malformed_date()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        // DateOnly.Parse lança FormatException, que não estava na lista de
        // capturas: erro do cliente virava 500, contado como falha nossa.
        var response = await client.GetAsync("/api/nps/projects?from=abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// F6: os KPIs têm de ver a MESMA carteira que a tabela logo abaixo. O
    /// dashboard ignorava a dispensa, então o projeto sumia da lista e seguia
    /// somando no NPS oficial — e o drill-down de F8 não fechava com ele.
    /// </summary>
    [Fact]
    public async Task Dashboard_should_exclude_dismissed_projects_by_default()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Kpi{Guid.NewGuid():N}";
        var project = await CreateProjectAsync(client, marker);
        var token = await CreateGenericTokenAsync(client, project.Id);
        await SubmitAsync(client, token, 10);

        var antes = await GetDashboardAsync(client, marker);
        await client.PostAsJsonAsync($"/api/nps/projects/{project.Id}/collection-waiver",
            new DismissNpsCollectionRequest("fora do escopo"));
        var depois = await GetDashboardAsync(client, marker);
        var mostrando = await GetDashboardAsync(client, marker, includeDismissed: true);

        Assert.Equal(1, antes.TotalResponses);
        Assert.Equal(0, depois.TotalResponses);
        Assert.Equal(1, mostrando.TotalResponses);
    }

    /// <summary>
    /// B12: link vencido não coleta mais. Contá-lo como ativo punha o mesmo
    /// projeto em "Links ativos" e em "Projetos vencidos" na mesma barra.
    /// </summary>
    [Fact]
    public async Task Dashboard_should_not_count_an_expired_link_as_active()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Vencido{Guid.NewGuid():N}";
        var project = await CreateProjectAsync(client, marker);
        await CreateGenericTokenAsync(client, project.Id);

        var antes = await GetDashboardAsync(client, marker);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "update nps_dispatches set expires_at = now() - interval '1 day' where project_id = {0}",
                project.Id);
        }

        var depois = await GetDashboardAsync(client, marker);

        Assert.Equal(1, antes.ActiveDispatches);
        Assert.Equal(0, depois.ActiveDispatches);
        Assert.Equal(1, depois.OverdueProjects);
    }

    private static async Task<NpsDashboardResponse> GetDashboardAsync(HttpClient client, string marker, bool includeDismissed = false)
        => (await client.GetFromJsonAsync<NpsDashboardResponse>(
            $"/api/nps/dashboard?search={marker}&includeDismissed={includeDismissed.ToString().ToLowerInvariant()}"))!;

    private static async Task SubmitAsync(HttpClient client, Guid token, int score)
    {
        var response = await client.PostAsJsonAsync($"/api/nps/public/{token}/responses", new SubmitNpsSurveyResponseRequest(
            score, null, null, null, null, null, null, null, null));
        response.EnsureSuccessStatusCode();
    }

    private static async Task<Guid> CreateGenericTokenAsync(HttpClient client, int projectId)
    {
        var response = await client.PostAsJsonAsync("/api/nps/dispatches", new CreateNpsDispatchRequest(
            ProjectId: projectId,
            PeriodStart: "2026-06-01",
            PeriodEnd: "2026-06-30",
            Format: "Simplificado",
            Language: "Português",
            CreatedBy: "ops@example.com",
            ContactIds: [],
            CreateGenericToken: true));
        response.EnsureSuccessStatusCode();
        var detail = (await response.Content.ReadFromJsonAsync<NpsDispatchDetailResponse>())!;
        return detail.Targets.Single(t => t.IsGeneric).Token;
    }

    /// <summary>
    /// B12: link vencido não coleta mais, então o projeto não está "Link
    /// gerado". Sem isto o KPI dizia "Links ativos 0" e a linha, na mesma
    /// tela, dizia que havia link — e ?status=Link gerado ainda a selecionava.
    /// </summary>
    [Fact]
    public async Task An_expired_link_should_not_leave_the_project_as_link_sent()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"StatusVencido{Guid.NewGuid():N}";
        var project = await CreateProjectAsync(client, marker);
        await CreateGenericTokenAsync(client, project.Id);

        var antes = await ListAsync(client, $"search={marker}");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "update nps_dispatches set expires_at = now() - interval '1 day' where project_id = {0}",
                project.Id);
        }

        var depois = await ListAsync(client, $"search={marker}");

        Assert.Equal("Link gerado", antes.Single().CollectionStatus);
        Assert.Equal("Pendente", depois.Single().CollectionStatus);
    }

    [Fact]
    public async Task ListProjects_should_reject_an_unknown_facet_value()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/nps/projects?dc=DC1&dc=DC99");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FilterOptions_should_list_the_distinct_companies_and_delivery_managers()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Opcoes{Guid.NewGuid():N}";
        var company = $"Empresa {marker}";

        await CreateProjectAsync(client, marker, company: company, deliveryManager: $"DM {marker}");
        await CreateProjectAsync(client, marker, company: company, deliveryManager: $"DM {marker}");

        var options = await client.GetFromJsonAsync<NpsFilterOptionsResponse>("/api/nps/filter-options");

        Assert.NotNull(options);
        Assert.Single(options!.Companies, c => c == company);
        Assert.Single(options.DeliveryManagers, d => d == $"DM {marker}");
    }

    private static async Task<IReadOnlyList<NpsProjectResponse>> ListAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync($"/api/nps/projects?{query}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<NpsProjectResponse>>())!;
    }

    private static async Task<ProjectResponse> CreateProjectAsync(
        HttpClient client,
        string marker,
        string dc = "DC1",
        string type = "Squad",
        string company = "Client",
        string deliveryManager = "Maria")
    {
        var response = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest(
            Dc: dc,
            Status: "Em andamento",
            Name: $"{marker} {Guid.NewGuid():N}",
            Client: company,
            Type: type,
            StartDate: "2026-01-01",
            EndDate: "2026-12-31",
            DeliveryManager: deliveryManager,
            Renewal: "None",
            RenewalObservation: null));

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectResponse>())!;
    }

    private static async Task<int> CreateDispatchAsync(HttpClient client, int projectId)
    {
        var response = await client.PostAsJsonAsync("/api/nps/dispatches", new CreateNpsDispatchRequest(
            ProjectId: projectId,
            PeriodStart: "2026-06-01",
            PeriodEnd: "2026-06-30",
            Format: "Simplificado",
            Language: "Português",
            CreatedBy: "ops@example.com",
            ContactIds: [],
            CreateGenericToken: true));

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NpsDispatchDetailResponse>())!.Dispatch.Id;
    }
}
