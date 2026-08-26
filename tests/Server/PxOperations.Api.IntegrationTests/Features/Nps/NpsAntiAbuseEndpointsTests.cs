using System.Net;
using System.Net.Http.Json;
using PxOperations.Api.Features.Nps;
using PxOperations.Api.Features.Nps.Contracts;
using PxOperations.Api.Features.Projects.Contracts;
using PxOperations.Api.IntegrationTests.Infrastructure;

namespace PxOperations.Api.IntegrationTests.Features.Nps;

/// <summary>
/// B4: com o link aberto, o uso único deixou de ser o freio. F4 pede três, e
/// nenhum é à prova de bala — quem quiser burlar troca de navegador ou de rede.
/// A proteção é proporcional, como o PRD diz: barra reenvio acidental e abuso
/// em massa, não um adversário determinado.
///
/// O bloqueio por navegador vive no cliente (localStorage) e é coberto pelos
/// testes de componente; aqui ficam os dois que são do servidor.
/// </summary>
[Collection(ApiIntegrationCollection.Name)]
public sealed class NpsAntiAbuseEndpointsTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Submitting_twice_with_the_same_email_should_conflict()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var token = await CreateGenericTokenAsync(client);

        var first = await SubmitAsync(client, token, email: "ana@cliente.com");
        var second = await SubmitAsync(client, token, email: " ANA@Cliente.com ");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        // Aparado e sem distinguir maiúsculas: "ANA@Cliente.com " é a mesma
        // pessoa de "ana@cliente.com", e um dedupe literal não pegaria.
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Two_anonymous_responses_should_both_be_accepted()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var token = await CreateGenericTokenAsync(client);

        // D1/D4: é o coração do link compartilhado. Sem e-mail informado não há
        // como deduplicar, e nem se quer: a resposta anônima é a regra.
        var first = await SubmitAsync(client, token, email: null);
        var second = await SubmitAsync(client, token, email: null);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    /// <summary>
    /// O link de um projeto dispensado continua válido — a dispensa é decisão
    /// interna, e quem recebeu o link não sabe dela. Responder tem de funcionar;
    /// falhar DEPOIS de gravar faria a pessoa tentar de novo e duplicar.
    /// </summary>
    [Fact]
    public async Task Submitting_on_a_dismissed_project_should_still_succeed()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var token = await CreateGenericTokenAsync(client);
        var projectId = (await client.GetFromJsonAsync<List<NpsProjectResponse>>("/api/nps/projects"))!
            .Max(p => p.Id);

        (await client.PostAsJsonAsync($"/api/nps/projects/{projectId}/collection-waiver",
            new DismissNpsCollectionRequest("dispensado antes de responder"))).EnsureSuccessStatusCode();

        var response = await SubmitAsync(client, token, email: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Submitting_beyond_the_ip_limit_should_return_too_many_requests()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var token = await CreateGenericTokenAsync(client);

        HttpResponseMessage? last = null;
        for (var i = 0; i <= AntiAbuse.SubmitPermitLimit; i++)
        {
            last = await SubmitAsync(client, token, email: null);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    private static Task<HttpResponseMessage> SubmitAsync(HttpClient client, Guid token, string? email)
        => client.PostAsJsonAsync($"/api/nps/public/{token}/responses", new SubmitNpsSurveyResponseRequest(
            Score: 9,
            BusinessValue: null,
            Schedule: null,
            Quality: null,
            Communication: null,
            Tags: null,
            Comment: null,
            RespondentName: null,
            RespondentEmail: email));

    private static async Task<Guid> CreateGenericTokenAsync(HttpClient client)
    {
        var project = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest(
            Dc: "DC1",
            Status: "Em andamento",
            Name: $"Antiabuso {Guid.NewGuid():N}",
            Client: "Client",
            Type: "Squad",
            StartDate: "2026-01-01",
            EndDate: "2026-12-31",
            DeliveryManager: "Maria",
            Renewal: "None",
            RenewalObservation: null));
        project.EnsureSuccessStatusCode();
        var created = (await project.Content.ReadFromJsonAsync<ProjectResponse>())!;

        var dispatch = await client.PostAsJsonAsync("/api/nps/dispatches", new CreateNpsDispatchRequest(
            ProjectId: created.Id,
            PeriodStart: "2026-06-01",
            PeriodEnd: "2026-06-30",
            Format: "Simplificado",
            Language: "Português",
            CreatedBy: "ops@example.com",
            ContactIds: [],
            CreateGenericToken: true));
        dispatch.EnsureSuccessStatusCode();
        var detail = (await dispatch.Content.ReadFromJsonAsync<NpsDispatchDetailResponse>())!;

        return detail.Targets.Single(t => t.IsGeneric).Token;
    }
}
