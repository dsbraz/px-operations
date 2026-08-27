using System.Net.Http.Json;
using PxOperations.Api.Features.Nps.Contracts;
using PxOperations.Api.Features.Projects.Contracts;
using PxOperations.Api.IntegrationTests.Infrastructure;

namespace PxOperations.Api.IntegrationTests.Features.Nps;

/// <summary>
/// B11/F9: médias por aspecto, escala de 1 a 5, só do formato Completo. Cada
/// aspecto é OPCIONAL mesmo no Completo, então cada um tem o seu próprio
/// denominador — média sobre um denominador emprestado mente.
/// </summary>
[Collection(ApiIntegrationCollection.Name)]
public sealed class NpsAspectAveragesTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Averages_should_use_each_aspect_own_denominator()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Aspecto{Guid.NewGuid():N}";
        var token = await CreateTokenAsync(client, marker, "Completo");

        // Qualidade: 5 e 3 → 4,0 em 2 respostas.
        // Prazos: só 2 → 2,0 em 1 resposta. Contar o pulado como zero daria 1,0.
        await SubmitAsync(client, token, score: 9, quality: 5, schedule: 2);
        await SubmitAsync(client, token, score: 8, quality: 3, schedule: null);

        var dashboard = await GetAsync(client, marker);

        Assert.Equal(4.0m, dashboard.QualityAverage);
        Assert.Equal(2, dashboard.QualityCount);
        Assert.Equal(2.0m, dashboard.ScheduleAverage);
        Assert.Equal(1, dashboard.ScheduleCount);
    }

    [Fact]
    public async Task An_aspect_nobody_answered_should_have_no_average()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Vazio{Guid.NewGuid():N}";
        var token = await CreateTokenAsync(client, marker, "Completo");

        await SubmitAsync(client, token, score: 9, quality: 4, schedule: null);

        var dashboard = await GetAsync(client, marker);

        // Nulo, não zero: zero é uma nota que ninguém deu, e a escala começa em 1.
        Assert.Null(dashboard.CommunicationAverage);
        Assert.Equal(0, dashboard.CommunicationCount);
    }

    [Fact]
    public async Task A_simplified_response_should_not_enter_the_averages()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Simples{Guid.NewGuid():N}";
        var project = await CreateProjectAsync(client, marker);
        var completo = await CreateDispatchAsync(client, project.Id, "Completo");
        var simplificado = await CreateDispatchAsync(client, project.Id, "Simplificado");

        await SubmitAsync(client, completo, score: 9, quality: 5, schedule: 5);
        // O Simplificado nem coleta aspecto — o servidor descarta o que vier.
        await SubmitAsync(client, simplificado, score: 2, quality: 1, schedule: 1);

        var dashboard = await GetAsync(client, marker);

        Assert.Equal(5.0m, dashboard.QualityAverage);
        Assert.Equal(1, dashboard.QualityCount);
        Assert.Equal(1, dashboard.CompleteResponses);
    }

    [Fact]
    public async Task Averages_should_follow_the_active_facets()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Recorte{Guid.NewGuid():N}";
        var dc1 = await CreateProjectAsync(client, marker, dc: "DC1");
        var dc2 = await CreateProjectAsync(client, marker, dc: "DC2");

        await SubmitAsync(client, await CreateDispatchAsync(client, dc1.Id, "Completo"), score: 9, quality: 5, schedule: 5);
        await SubmitAsync(client, await CreateDispatchAsync(client, dc2.Id, "Completo"), score: 3, quality: 1, schedule: 1);

        var todos = await GetAsync(client, marker);
        var soDc1 = await GetAsync(client, marker, "&dc=DC1");

        // F9 pede o recorte explícito no subtítulo; para isso ele tem de VALER.
        Assert.Equal(3.0m, todos.QualityAverage);
        Assert.Equal(5.0m, soDc1.QualityAverage);
        Assert.Equal(1, soDc1.CompleteResponses);
    }

    private static async Task<NpsDashboardResponse> GetAsync(HttpClient client, string marker, string extra = "")
        => (await client.GetFromJsonAsync<NpsDashboardResponse>($"/api/nps/dashboard?search={marker}{extra}"))!;

    private static async Task SubmitAsync(HttpClient client, Guid token, int score, int? quality, int? schedule)
    {
        var response = await client.PostAsJsonAsync($"/api/nps/public/{token}/responses", new SubmitNpsSurveyResponseRequest(
            Score: score,
            BusinessValue: null,
            Schedule: schedule,
            Quality: quality,
            Communication: null,
            Tags: null,
            Comment: null,
            RespondentName: null,
            RespondentEmail: null));
        response.EnsureSuccessStatusCode();
    }

    private static async Task<Guid> CreateTokenAsync(HttpClient client, string marker, string format)
    {
        var project = await CreateProjectAsync(client, marker);
        return await CreateDispatchAsync(client, project.Id, format);
    }

    private static async Task<ProjectResponse> CreateProjectAsync(HttpClient client, string marker, string dc = "DC1")
    {
        var response = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest(
            Dc: dc,
            Status: "Em andamento",
            Name: $"{marker} {Guid.NewGuid():N}",
            Client: "Client",
            Type: "Squad",
            StartDate: "2026-01-01",
            EndDate: "2026-12-31",
            DeliveryManager: "Maria",
            Renewal: "None",
            RenewalObservation: null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectResponse>())!;
    }

    private static async Task<Guid> CreateDispatchAsync(HttpClient client, int projectId, string format)
    {
        var response = await client.PostAsJsonAsync("/api/nps/dispatches", new CreateNpsDispatchRequest(
            ProjectId: projectId,
            PeriodStart: "2026-06-01",
            PeriodEnd: "2026-06-30",
            Format: format,
            Language: "Português",
            CreatedBy: "ops@example.com",
            ContactIds: [],
            CreateGenericToken: true));
        response.EnsureSuccessStatusCode();
        var detail = (await response.Content.ReadFromJsonAsync<NpsDispatchDetailResponse>())!;
        return detail.Targets.Single(t => t.IsGeneric).Token;
    }
}
