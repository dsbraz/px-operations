using System.Net;
using System.Net.Http.Json;
using PxOperations.Api.Features.Nps.Contracts;
using PxOperations.Api.Features.Projects.Contracts;
using PxOperations.Api.IntegrationTests.Infrastructure;

namespace PxOperations.Api.IntegrationTests.Features.Nps;

/// <summary>
/// B6: a listagem de respostas em JSON. A consulta já existia, mas só saía
/// pelo CSV — sem este endpoint nem a tabela de auditoria (F10) nem o
/// drill-down por projeto (F8) têm de onde ler.
/// </summary>
[Collection(ApiIntegrationCollection.Name)]
public sealed class NpsResponsesEndpointTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task ListResponses_should_return_the_most_recent_first()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Ordem{Guid.NewGuid():N}";
        var token = await CreateTokenAsync(client, marker, "Simplificado");

        await SubmitAsync(client, token, score: 3, comment: "primeira");
        await SubmitAsync(client, token, score: 9, comment: "segunda");

        var responses = await ListAsync(client, $"search={marker}");

        // F10 exibe da mais recente para a mais antiga; a ordem é do contrato,
        // não da tela, senão cada consumidor reordena por conta.
        Assert.Equal(2, responses.Count);
        Assert.Equal("segunda", responses[0].Comment);
        Assert.Equal("primeira", responses[1].Comment);
    }

    [Fact]
    public async Task ListResponses_should_filter_by_format()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Formato{Guid.NewGuid():N}";
        var project = await CreateProjectAsync(client, marker);
        var simplificado = await CreateDispatchAsync(client, project.Id, "Simplificado");
        var completo = await CreateDispatchAsync(client, project.Id, "Completo");

        await SubmitAsync(client, simplificado, score: 7, comment: "do simplificado");
        await SubmitAsync(client, completo, score: 10, comment: "do completo");

        var responses = await ListAsync(client, $"search={marker}&format=Completo");

        Assert.Single(responses);
        Assert.Equal("do completo", responses[0].Comment);
        // B5: o formato vem por resposta, e é o que a coluna Formato de F10 usa.
        Assert.Equal("Completo", responses[0].Format);
    }

    [Fact]
    public async Task ListResponses_should_return_the_union_of_two_classifications()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var marker = $"Classe{Guid.NewGuid():N}";
        var token = await CreateTokenAsync(client, marker, "Simplificado");

        await SubmitAsync(client, token, score: 3, comment: "detrator");
        await SubmitAsync(client, token, score: 8, comment: "neutro");
        await SubmitAsync(client, token, score: 10, comment: "promotor");

        var responses = await ListAsync(client, $"search={marker}&classification=Detrator&classification=Promotor");

        Assert.Equal(2, responses.Count);
        Assert.DoesNotContain(responses, r => r.Comment == "neutro");
    }

    [Fact]
    public async Task ListResponses_should_reject_an_unknown_format()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/nps/responses?format=Telepatia");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<IReadOnlyList<NpsSurveyResponse>> ListAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync($"/api/nps/responses?{query}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<NpsSurveyResponse>>())!;
    }

    private static async Task SubmitAsync(HttpClient client, Guid token, int score, string comment)
    {
        var response = await client.PostAsJsonAsync($"/api/nps/public/{token}/responses", new SubmitNpsSurveyResponseRequest(
            Score: score,
            BusinessValue: null,
            Schedule: null,
            Quality: null,
            Communication: null,
            Tags: null,
            Comment: comment,
            RespondentName: null,
            RespondentEmail: null));
        response.EnsureSuccessStatusCode();
    }

    private static async Task<Guid> CreateTokenAsync(HttpClient client, string marker, string format)
    {
        var project = await CreateProjectAsync(client, marker);
        return await CreateDispatchAsync(client, project.Id, format);
    }

    private static async Task<ProjectResponse> CreateProjectAsync(HttpClient client, string marker)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest(
            Dc: "DC1",
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
