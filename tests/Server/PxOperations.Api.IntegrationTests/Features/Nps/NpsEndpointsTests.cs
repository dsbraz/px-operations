using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.Api.Features.Nps.Contracts;
using PxOperations.Api.Features.Projects.Contracts;
using PxOperations.Api.IntegrationTests.Infrastructure;
using PxOperations.Infrastructure.Persistence;

namespace PxOperations.Api.IntegrationTests.Features.Nps;

[Collection(ApiIntegrationCollection.Name)]
public sealed class NpsEndpointsTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Migration_should_enforce_one_response_per_token_atomically()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select indexdef
            from pg_indexes
            where tablename = 'nps_survey_responses'
              and indexname = 'IX_nps_survey_responses_target_id'
            """;

        var indexDefinition = Assert.IsType<string>(await command.ExecuteScalarAsync());
        Assert.Contains("UNIQUE", indexDefinition);
        Assert.DoesNotContain("WHERE", indexDefinition);
    }

    [Fact]
    public async Task Contact_crud_should_archive_on_delete()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "NPS Contacts");

        var create = await client.PostAsJsonAsync($"/api/nps/projects/{project.Id}/contacts", new CreateNpsContactRequest("Ana Cliente", "ana@example.com", "Sponsor"));
        var contact = await create.Content.ReadFromJsonAsync<NpsContactResponse>();

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.NotNull(contact);

        var update = await client.PatchAsJsonAsync($"/api/nps/contacts/{contact!.Id}", new UpdateNpsContactRequest("Ana Silva", "ana.silva@example.com", "Diretora"));
        var updated = await update.Content.ReadFromJsonAsync<NpsContactResponse>();

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Ana Silva", updated!.Name);

        var delete = await client.DeleteAsync($"/api/nps/contacts/{contact.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var list = await client.GetFromJsonAsync<List<NpsContactResponse>>($"/api/nps/projects/{project.Id}/contacts?includeArchived=true");
        Assert.Contains(list!, c => c.Id == contact.Id && c.IsArchived);
    }

    [Fact]
    public async Task Public_link_should_allow_one_simplified_response_with_null_dimensions()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "NPS Token");
        var dispatch = await CreateDispatchAsync(client, project.Id, [], createGeneric: true);
        var token = dispatch.Targets.Single().Token;

        var survey = await client.GetFromJsonAsync<NpsPublicSurveyResponse>($"/api/nps/public/{token}");
        Assert.Equal(project.Id, survey!.ProjectId);
        Assert.False(survey.AlreadyAnswered);

        var projectsWithOpenLink = await client.GetFromJsonAsync<List<NpsProjectResponse>>($"/api/nps/projects?search={Uri.EscapeDataString(project.Name)}");
        var openLinkProject = Assert.Single(projectsWithOpenLink!);
        Assert.Equal(1, openLinkProject.LinkTargetsCount);
        Assert.Equal(0, openLinkProject.AnsweredLinkTargetsCount);

        var submit = await client.PostAsJsonAsync($"/api/nps/public/{token}/responses", new SubmitNpsSurveyResponseRequest(
            Score: 9,
            Scope: 1,
            Schedule: 2,
            Quality: 3,
            Communication: 4,
            Tags: "ignored",
            Comment: "Muito bom",
            RespondentName: "Ana",
            RespondentEmail: "ana@example.com"));
        var response = await submit.Content.ReadFromJsonAsync<NpsSurveyResponse>();

        Assert.Equal(HttpStatusCode.Created, submit.StatusCode);
        Assert.Equal("Promotor", response!.Classification);
        Assert.Null(response.Scope);
        Assert.Null(response.Tags);

        var projectsWithAnsweredLink = await client.GetFromJsonAsync<List<NpsProjectResponse>>($"/api/nps/projects?search={Uri.EscapeDataString(project.Name)}");
        var answeredLinkProject = Assert.Single(projectsWithAnsweredLink!);
        Assert.Equal(1, answeredLinkProject.LinkTargetsCount);
        Assert.Equal(1, answeredLinkProject.AnsweredLinkTargetsCount);

        var answeredSurvey = await client.GetFromJsonAsync<NpsPublicSurveyResponse>($"/api/nps/public/{token}");
        Assert.True(answeredSurvey!.AlreadyAnswered);

        var duplicate = await client.PostAsJsonAsync($"/api/nps/public/{token}/responses", new SubmitNpsSurveyResponseRequest(10, null, null, null, null, null, null, null, null));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Closed_dispatch_should_reject_public_response_and_dashboard_should_calculate_nps()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "NPS Dashboard");
        var promoterDispatch = await CreateDispatchAsync(client, project.Id, [], createGeneric: true);
        var promoterToken = promoterDispatch.Targets.Single().Token;
        var detractorDispatch = await CreateDispatchAsync(client, project.Id, [], createGeneric: true);
        var detractorToken = detractorDispatch.Targets.Single().Token;

        await client.PostAsJsonAsync($"/api/nps/public/{promoterToken}/responses", new SubmitNpsSurveyResponseRequest(10, null, null, null, null, null, "Promotor", null, null));
        await client.PostAsJsonAsync($"/api/nps/public/{detractorToken}/responses", new SubmitNpsSurveyResponseRequest(4, null, null, null, null, null, "Detrator", null, null));

        var dashboard = await client.GetFromJsonAsync<NpsDashboardResponse>($"/api/nps/dashboard?projectId={project.Id}");
        Assert.Equal(2, dashboard!.TotalResponses);
        Assert.Equal(0, dashboard.OfficialNps);
        Assert.Equal(1, dashboard.Promoters);
        Assert.Equal(1, dashboard.Detractors);

        var closedDispatch = await CreateDispatchAsync(client, project.Id, [], createGeneric: true);
        var closedToken = closedDispatch.Targets.Single().Token;

        var close = await client.PatchAsync($"/api/nps/dispatches/{closedDispatch.Dispatch.Id}/close", null);
        Assert.Equal(HttpStatusCode.OK, close.StatusCode);

        var rejected = await client.PostAsJsonAsync($"/api/nps/public/{closedToken}/responses", new SubmitNpsSurveyResponseRequest(8, null, null, null, null, null, null, null, null));
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
    }

    [Fact]
    public async Task Export_should_return_csv()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "NPS Export");
        var dispatch = await CreateDispatchAsync(client, project.Id, [], createGeneric: true);
        var token = dispatch.Targets.Single().Token;
        await client.PostAsJsonAsync($"/api/nps/public/{token}/responses", new SubmitNpsSurveyResponseRequest(9, null, null, null, null, null, "CSV", null, null));

        var export = await client.GetAsync($"/api/nps/responses/export?projectId={project.Id}");
        var csv = await export.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Contains("project_name", csv);
        Assert.Contains("NPS Export", csv);
    }

    private static async Task<ProjectResponse> CreateProjectAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest(
            Dc: "DC1",
            Status: "Em andamento",
            Name: $"{name} {Guid.NewGuid():N}",
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

    private static async Task<NpsDispatchDetailResponse> CreateDispatchAsync(HttpClient client, int projectId, IReadOnlyList<int> contactIds, bool createGeneric)
    {
        var response = await client.PostAsJsonAsync("/api/nps/dispatches", new CreateNpsDispatchRequest(
            ProjectId: projectId,
            PeriodStart: "2026-06-01",
            PeriodEnd: "2026-06-30",
            Format: "Simplificado",
            Language: "Português",
            CreatedBy: "ops@example.com",
            ContactIds: contactIds,
            CreateGenericToken: createGeneric));

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NpsDispatchDetailResponse>())!;
    }
}
