using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.Api.Features.Nps;
using PxOperations.Api.Features.Projects.Contracts;
using PxOperations.Api.IntegrationTests.Infrastructure;
using PxOperations.Application.Features.Nps;
using PxOperations.Domain.Nps;
using PxOperations.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PxOperations.Api.IntegrationTests.Features.Nps;

[Collection(ApiIntegrationCollection.Name)]
public sealed class NpsEndpointsTests(PostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset InitialNow = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Generic_link_should_accept_anonymous_responses_and_reject_the_same_normalized_email()
    {
        var time = new TestTimeProvider(InitialNow);
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, time);
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Generic responses");
        var dispatch = await CreateDispatchAsync(client, project.Id, "simplified");
        var token = dispatch.Targets.Single(target => target.IsGeneric).Token;

        var firstAnonymous = await SubmitAsync(client, token, 9);
        var secondAnonymous = await SubmitAsync(client, token, 10);
        var firstEmail = await SubmitAsync(client, token, 8, "  Person@Example.COM ");
        var duplicateEmail = await SubmitAsync(client, token, 7, "person@example.com");

        Assert.Equal(HttpStatusCode.Created, firstAnonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondAnonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Created, firstEmail.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateEmail.StatusCode);
        Assert.Equal("application/problem+json", duplicateEmail.Content.Headers.ContentType?.MediaType);

        var survey = await client.GetFromJsonAsync<NpsPublicSurveyView>($"/api/nps/public/{token}");
        Assert.Equal("open", survey!.Availability);
    }

    [Fact]
    public async Task Contact_target_should_remain_single_use()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, new TestTimeProvider(InitialNow));
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Contact response");
        var contactResponse = await client.PostAsJsonAsync(
            $"/api/nps/projects/{project.Id}/contacts",
            new CreateNpsContactRequest("Ana", "ana@example.com", "Sponsor"));
        var contact = await contactResponse.Content.ReadFromJsonAsync<NpsContactView>();
        var dispatch = await CreateDispatchAsync(client, project.Id, "complete", [contact!.Id]);
        var token = dispatch.Targets.Single(target => target.ContactId == contact.Id).Token;

        var first = await SubmitAsync(client, token, 9);
        var repeated = await SubmitAsync(client, token, 10);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        var survey = await client.GetFromJsonAsync<NpsPublicSurveyView>($"/api/nps/public/{token}");
        Assert.Equal("already_answered", survey!.Availability);
    }

    [Fact]
    public async Task Exactly_twenty_days_should_make_the_link_expired_but_still_queryable()
    {
        var time = new TestTimeProvider(InitialNow);
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, time);
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Expired response");
        var dispatch = await CreateDispatchAsync(client, project.Id, "simplified");
        var token = dispatch.Targets.Single(target => target.IsGeneric).Token;
        time.Set(InitialNow.AddDays(20));

        var get = await client.GetAsync($"/api/nps/public/{token}");
        var survey = await get.Content.ReadFromJsonAsync<NpsPublicSurveyView>();
        var submit = await SubmitAsync(client, token, 9);

        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("expired", survey!.Availability);
        Assert.Equal(HttpStatusCode.Conflict, submit.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/nps/public/{Guid.NewGuid()}" )).StatusCode);
    }

    [Fact]
    public async Task Creating_a_new_round_should_close_only_the_same_format()
    {
        var time = new TestTimeProvider(InitialNow);
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, time);
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Round replacement");
        var firstComplete = await CreateDispatchAsync(client, project.Id, "complete");
        time.Set(InitialNow.AddDays(1));
        var simplified = await CreateDispatchAsync(client, project.Id, "simplified");
        time.Set(InitialNow.AddDays(2));
        var secondComplete = await CreateDispatchAsync(client, project.Id, "complete");

        var oldComplete = await client.GetFromJsonAsync<NpsDispatchDetailView>($"/api/nps/dispatches/{firstComplete.Dispatch.Id}");
        var openSimplified = await client.GetFromJsonAsync<NpsDispatchDetailView>($"/api/nps/dispatches/{simplified.Dispatch.Id}");
        var openComplete = await client.GetFromJsonAsync<NpsDispatchDetailView>($"/api/nps/dispatches/{secondComplete.Dispatch.Id}");

        Assert.Equal("closed", oldComplete!.Dispatch.Status);
        Assert.Equal("open", openSimplified!.Dispatch.Status);
        Assert.Equal("open", openComplete!.Dispatch.Status);
    }

    [Fact]
    public async Task First_complete_and_simplified_rounds_should_be_created_concurrently()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, new TestTimeProvider(InitialNow));
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Concurrent first rounds");

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/nps/dispatches", new CreateNpsDispatchRequest(project.Id, "complete", "pt", [])),
            client.PostAsJsonAsync("/api/nps/dispatches", new CreateNpsDispatchRequest(project.Id, "simplified", "en", [])));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        var dispatches = await client.GetFromJsonAsync<List<NpsDispatchView>>($"/api/nps/projects/{project.Id}/dispatches");
        Assert.Equal(2, dispatches!.Count);
        Assert.All(dispatches, dispatch => Assert.Equal("open", dispatch.Status));
    }

    [Fact]
    public async Task Waiver_should_hide_the_project_by_default_and_reactivation_should_preserve_it()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, new TestTimeProvider(InitialNow));
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Waiver");
        await CreateDispatchAsync(client, project.Id, "complete");

        var waive = await client.PostAsJsonAsync($"/api/nps/projects/{project.Id}/waiver", new WaiveNpsCollectionRequest("Sem pesquisa no contrato"));
        var repeated = await client.PostAsJsonAsync($"/api/nps/projects/{project.Id}/waiver", new WaiveNpsCollectionRequest("Outra"));
        var hidden = await client.GetFromJsonAsync<List<NpsProjectView>>($"/api/nps/projects?search={Uri.EscapeDataString(project.Name)}");
        var included = await client.GetFromJsonAsync<List<NpsProjectView>>($"/api/nps/projects?search={Uri.EscapeDataString(project.Name)}&includeWaived=true");
        var blockedDispatch = await client.PostAsJsonAsync("/api/nps/dispatches", new CreateNpsDispatchRequest(project.Id, "complete", "pt", []));

        Assert.Equal(HttpStatusCode.Created, waive.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        Assert.Empty(hidden!);
        Assert.Equal("waived", Assert.Single(included!).Stage.Code);
        Assert.Equal(HttpStatusCode.Conflict, blockedDispatch.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/nps/projects/{project.Id}/waiver")).StatusCode);
        var reactivated = await client.GetFromJsonAsync<NpsProjectDetailView>($"/api/nps/projects/{project.Id}");
        Assert.Null(reactivated!.Project.Waiver);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/nps/projects/{project.Id}/waiver")).StatusCode);
    }

    [Fact]
    public async Task Repeated_facets_should_use_or_inside_a_facet_and_and_between_facets()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, new TestTimeProvider(InitialNow));
        using var client = factory.CreateClient();
        var alpha = await CreateProjectAsync(client, "Facet Alpha", "Alpha", "DC1");
        await CreateProjectAsync(client, "Facet Beta", "Beta", "DC2");
        await CreateProjectAsync(client, "Facet Gamma", "Gamma", "DC1");

        var result = await client.GetFromJsonAsync<List<NpsProjectView>>(
            "/api/nps/projects?client=Alpha&client=Beta&dc=DC1");

        Assert.Equal(alpha.Id, Assert.Single(result!).Id);
    }

    [Fact]
    public async Task Response_period_should_use_submitted_at_and_csv_should_honor_all_response_filters()
    {
        var time = new TestTimeProvider(InitialNow);
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, time);
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Filtered results");
        var complete = await CreateDispatchAsync(client, project.Id, "complete");
        var completeToken = complete.Targets.Single(target => target.IsGeneric).Token;
        await SubmitAsync(client, completeToken, 9, comment: "included-row");
        time.Set(new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero));
        var simplified = await CreateDispatchAsync(client, project.Id, "simplified");
        var simplifiedToken = simplified.Targets.Single(target => target.IsGeneric).Token;
        await SubmitAsync(client, simplifiedToken, 4, comment: "excluded-row");

        var dashboard = await client.GetFromJsonAsync<NpsDashboardView>(
            $"/api/nps/dashboard?projectId={project.Id}&from=2026-09-01");
        var csv = await client.GetStringAsync(
            $"/api/nps/responses/export?projectId={project.Id}&format=complete&classification=promoter");

        Assert.Equal(1, dashboard!.TotalResponses);
        Assert.Equal(-100m, dashboard.OfficialNps);
        Assert.Contains("business_value", csv);
        Assert.Contains("included-row", csv);
        Assert.DoesNotContain("excluded-row", csv);
    }

    [Fact]
    public async Task Dashboard_should_reconcile_complete_aspect_averages_with_the_results_filters()
    {
        var time = new TestTimeProvider(InitialNow);
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, time);
        using var client = factory.CreateClient();
        var included = await CreateProjectAsync(client, "Aspect Included", "Aspect Client", "DC1");
        var excluded = await CreateProjectAsync(client, "Aspect Included Excluded", "Other Client", "DC2");
        var complete = await CreateDispatchAsync(client, included.Id, "complete");
        var simplified = await CreateDispatchAsync(client, included.Id, "simplified");
        var excludedComplete = await CreateDispatchAsync(client, excluded.Id, "complete");
        var completeToken = complete.Targets.Single(target => target.IsGeneric).Token;

        await SubmitAsync(client, completeToken, 10, quality: 1, schedule: 1, communication: 1, businessValue: 1);
        time.Set(new DateTimeOffset(2026, 8, 2, 12, 0, 0, TimeSpan.Zero));
        await SubmitAsync(client, completeToken, 10, quality: 5, schedule: 4, businessValue: 2);
        await SubmitAsync(client, completeToken, 9, quality: 4, communication: 3, businessValue: 3);
        time.Set(new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero));
        await SubmitAsync(client, completeToken, 8, quality: 2, schedule: 5, communication: 5);
        await SubmitAsync(client, simplified.Targets.Single(target => target.IsGeneric).Token, 1);
        await SubmitAsync(client, excludedComplete.Targets.Single(target => target.IsGeneric).Token, 10, quality: 5, schedule: 5, communication: 5, businessValue: 5);

        var dashboard = await client.GetFromJsonAsync<NpsDashboardView>(
            "/api/nps/dashboard?search=Aspect%20Included&client=Aspect%20Client&dc=DC1&projectType=squad&deliveryManager=Maria&status=responded&from=2026-08-02&to=2026-08-03");

        Assert.NotNull(dashboard);
        Assert.Equal(4, dashboard.TotalResponses);
        Assert.Equal(3, dashboard.AspectSummary.CompleteResponsesCount);
        Assert.Equal(1, dashboard.AspectSummary.Scale.Minimum);
        Assert.Equal(5, dashboard.AspectSummary.Scale.Maximum);
        Assert.Equal(
            new[] { "quality", "schedule", "communication", "business_value" },
            dashboard.AspectSummary.Aspects.Select(aspect => aspect.Code));
        Assert.Equal(
            new[] { "Qualidade técnica", "Prazos acordados", "Comunicação", "Valor para o negócio" },
            dashboard.AspectSummary.Aspects.Select(aspect => aspect.Label));
        Assert.Equal(new decimal?[] { 3.7m, 4.5m, 4.0m, 2.5m }, dashboard.AspectSummary.Aspects.Select(aspect => aspect.Average));
        Assert.Equal(new[] { 3, 2, 2, 2 }, dashboard.AspectSummary.Aspects.Select(aspect => aspect.ResponsesCount));
    }

    [Fact]
    public async Task Dashboard_should_return_an_empty_aspect_summary_when_only_simplified_responses_match()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, new TestTimeProvider(InitialNow));
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Simplified aspect summary");
        var dispatch = await CreateDispatchAsync(client, project.Id, "simplified");
        await SubmitAsync(client, dispatch.Targets.Single(target => target.IsGeneric).Token, 9);

        var dashboard = await client.GetFromJsonAsync<NpsDashboardView>($"/api/nps/dashboard?projectId={project.Id}");

        Assert.NotNull(dashboard);
        Assert.Equal(1, dashboard.TotalResponses);
        Assert.Equal(0, dashboard.AspectSummary.CompleteResponsesCount);
        Assert.All(dashboard.AspectSummary.Aspects, aspect =>
        {
            Assert.Null(aspect.Average);
            Assert.Equal(0, aspect.ResponsesCount);
        });
    }

    [Fact]
    public async Task Phase_two_queries_should_reconcile_project_results_responses_and_csv()
    {
        var time = new TestTimeProvider(InitialNow);
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, time);
        using var client = factory.CreateClient();
        var responded = await CreateProjectAsync(client, "Result Responded", "Result Client", "DC1");
        var linked = await CreateProjectAsync(client, "Result Linked", "Result Client", "DC1");
        var pending = await CreateProjectAsync(client, "Result Pending", "Other Client", "DC2");
        var complete = await CreateDispatchAsync(client, responded.Id, "complete");
        await CreateDispatchAsync(client, linked.Id, "simplified");
        var token = complete.Targets.Single(target => target.IsGeneric).Token;

        await SubmitAsync(client, token, 10, "person@example.com", "excellent response", "Person", 5, 4, 3, 2);
        time.Set(InitialNow.AddDays(1));
        await SubmitAsync(client, token, 4, comment: "needs attention");

        var results = await client.GetFromJsonAsync<List<NpsProjectResultView>>(
            "/api/nps/project-results?client=Result%20Client&client=Other%20Client&dc=DC1&status=responded&status=link_generated");
        var audit = await client.GetFromJsonAsync<List<NpsResponseView>>(
            $"/api/nps/responses?projectId={responded.Id}&search=Person&format=complete&classification=promoter&from=2026-08-01&to=2026-08-01");
        var projectResponses = await client.GetFromJsonAsync<List<NpsResponseView>>(
            $"/api/nps/projects/{responded.Id}/responses?classification=promoter&from=2026-08-01&to=2026-08-01");
        var csv = await client.GetStringAsync(
            $"/api/nps/responses/export?projectId={responded.Id}&search=Person&format=complete&classification=promoter&from=2026-08-01&to=2026-08-01");
        var options = await client.GetFromJsonAsync<NpsFilterOptionsView>("/api/nps/filter-options");

        Assert.Equal(2, results!.Count);
        var respondedResult = results.Single(result => result.Id == responded.Id);
        Assert.Equal(2, respondedResult.ResponsesCount);
        Assert.Equal(0m, respondedResult.OfficialNps);
        Assert.Equal(new[] { 1, 0, 1 }, respondedResult.Distribution.Select(item => item.Count));
        Assert.Equal("responded", respondedResult.Status.Code);
        Assert.Equal("link_generated", results.Single(result => result.Id == linked.Id).Status.Code);
        Assert.DoesNotContain(results, result => result.Id == pending.Id);
        Assert.Equal(audit!.Select(response => response.Id), projectResponses!.Select(response => response.Id));
        Assert.Contains("excellent response", csv);
        Assert.DoesNotContain("needs attention", csv);
        Assert.Equal(new[] { "responded", "link_generated", "pending" }, options!.Statuses.Select(option => option.Code));
    }

    [Fact]
    public async Task Project_results_period_should_exclude_projects_without_responses_in_the_operation_interval()
    {
        var time = new TestTimeProvider(new DateTimeOffset(2026, 8, 1, 23, 59, 0, TimeSpan.Zero));
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, time);
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "UTC interval");
        var dispatch = await CreateDispatchAsync(client, project.Id, "simplified");
        var token = dispatch.Targets.Single(target => target.IsGeneric).Token;
        await SubmitAsync(client, token, 9, comment: "inside-inclusive-day");

        var inside = await client.GetFromJsonAsync<List<NpsProjectResultView>>(
            $"/api/nps/project-results?projectId={project.Id}&from=2026-08-01&to=2026-08-01");
        var outside = await client.GetFromJsonAsync<List<NpsProjectResultView>>(
            $"/api/nps/project-results?projectId={project.Id}&from=2026-08-02&to=2026-08-02");

        Assert.Single(inside!);
        Assert.Empty(outside!);
    }

    [Fact]
    public async Task Sixty_first_submission_for_the_same_token_and_ip_should_return_problem_details_429()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, new TestTimeProvider(InitialNow));
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Rate limit");
        var dispatch = await CreateDispatchAsync(client, project.Id, "simplified");
        var token = dispatch.Targets.Single(target => target.IsGeneric).Token;

        for (var attempt = 0; attempt < 60; attempt++)
        {
            var accepted = await SubmitAsync(client, token, 9);
            Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        }

        var limited = await SubmitAsync(client, token, 9);
        var problem = await limited.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal(429, problem!.Status);
    }

    [Fact]
    public async Task Invalid_query_and_domain_conflict_should_use_problem_details()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, new TestTimeProvider(InitialNow));
        using var client = factory.CreateClient();

        var invalid = await client.GetAsync("/api/nps/dashboard?from=2026-09-01&to=2026-08-01");
        var invalidStatus = await client.GetAsync("/api/nps/project-results?status=awaiting_response");
        var problem = await invalid.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(400, problem!.Status);
        Assert.Equal("application/problem+json", invalid.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);
    }

    [Fact]
    public async Task Configured_development_client_origin_should_be_allowed()
    {
        const string origin = "http://localhost:18080";
        await using var factory = new ApiWebApplicationFactory(
            fixture.ConnectionString,
            new TestTimeProvider(InitialNow),
            origin);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/nps/projects");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.Contains(origin, response.Headers.GetValues("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Migration_should_have_scoped_uniqueness_and_range_constraints()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT string_agg(indexdef, E'\n' ORDER BY indexname)
            FROM pg_indexes
            WHERE tablename IN ('nps_dispatches', 'nps_survey_responses')
              AND indexdef LIKE '%UNIQUE%';
            """;
        var indexes = Assert.IsType<string>(await command.ExecuteScalarAsync());

        Assert.Contains("normalized_respondent_email", indexes);
        Assert.Contains("contact_id IS NOT NULL", indexes);
        Assert.Contains("collection_id, format", indexes);
    }

    [Fact]
    public async Task Historical_nps_data_should_be_converted_without_changing_ids()
    {
        await using var container = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("pxoperations_history")
            .WithUsername("pxoperations")
            .WithPassword("pxoperations")
            .Build();
        await container.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(container.GetConnectionString())
            .Options;
        await using var dbContext = new AppDbContext(options);
        var migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync("20260629202538_AddNpsModule");
        await dbContext.Database.ExecuteSqlRawAsync("""
            INSERT INTO projects (id, dc, status, name, client, type, renewal)
            VALUES (101, 0, 0, 'Projeto histórico', 'Cliente histórico', 0, 0);

            INSERT INTO nps_dispatches
                (id, project_id, period_start, period_end, format, language, status, created_by, created_at, closed_at)
            VALUES
                (201, 101, DATE '2026-01-01', DATE '2026-01-31', 0, 0, 0, 'legacy', TIMESTAMPTZ '2026-01-01 12:00:00Z', NULL),
                (202, 101, DATE '2026-02-01', DATE '2026-02-28', 0, 0, 0, 'legacy', TIMESTAMPTZ '2026-02-01 12:00:00Z', NULL);

            INSERT INTO nps_dispatch_targets (id, project_id, dispatch_id, contact_id, token, created_at)
            VALUES (301, 101, 201, NULL, '00000000-0000-0000-0000-000000000301', TIMESTAMPTZ '2026-01-01 12:00:00Z');

            INSERT INTO nps_survey_responses
                (id, project_id, dispatch_id, target_id, contact_id, score, classification, scope, schedule, quality,
                 communication, tags, comment, respondent_name, respondent_email, submitted_at)
            VALUES
                (401, 101, 201, 301, NULL, 0, 2, 9, 7, 10, 0, 'legacy-tag', 'Comentário', 'Pessoa',
                 '  Person@Example.COM ', TIMESTAMPTZ '2026-01-10 12:00:00Z');
            """);

        await migrator.MigrateAsync();

        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        await dbContext.Database.OpenConnectionAsync();
        command.CommandText = """
            SELECT response.id,
                   response.score,
                   response.classification,
                   response.quality,
                   response.schedule,
                   response.communication,
                   response.business_value,
                   response.normalized_respondent_email,
                   dispatch.id,
                   dispatch.expires_at,
                   dispatch.closed_at,
                   collection.project_id
            FROM nps_survey_responses AS response
            JOIN nps_dispatches AS dispatch ON dispatch.id = response.dispatch_id
            JOIN nps_collections AS collection ON collection.id = dispatch.collection_id
            WHERE response.id = 401;
            """;
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal(401, reader.GetInt32(0));
        Assert.Equal(1, reader.GetInt32(1));
        Assert.Equal(0, reader.GetInt32(2));
        Assert.Equal(5, reader.GetInt32(3));
        Assert.Equal(4, reader.GetInt32(4));
        Assert.Equal(1, reader.GetInt32(5));
        Assert.Equal(5, reader.GetInt32(6));
        Assert.Equal("person@example.com", reader.GetString(7));
        Assert.Equal(201, reader.GetInt32(8));
        Assert.Equal(new DateTime(2026, 1, 21, 12, 0, 0, DateTimeKind.Utc), reader.GetDateTime(9));
        Assert.Equal(new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc), reader.GetDateTime(10));
        Assert.Equal(101, reader.GetInt32(11));
    }

    [Fact]
    public async Task Csv_export_should_neutralize_spreadsheet_formulas_written_by_respondents()
    {
        var time = new TestTimeProvider(InitialNow);
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, time);
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Formula export");
        var dispatch = await CreateDispatchAsync(client, project.Id, "simplified");
        var token = dispatch.Targets.Single(target => target.IsGeneric).Token;
        await SubmitAsync(
            client,
            token,
            9,
            comment: "=HYPERLINK(\"http://evil\",\"clique aqui\")",
            name: "+49512345");

        var csv = await client.GetStringAsync($"/api/nps/responses/export?projectId={project.Id}");

        Assert.DoesNotContain("\"=HYPERLINK", csv, StringComparison.Ordinal);
        Assert.DoesNotContain("\"+49512345", csv, StringComparison.Ordinal);
        Assert.Contains("HYPERLINK", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Repository_should_recognize_the_database_duplicate_response_violation()
    {
        var time = new TestTimeProvider(InitialNow);
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, time);
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Duplicate race");
        var dispatch = await CreateDispatchAsync(client, project.Id, "simplified");
        var target = dispatch.Targets.Single(item => item.IsGeneric);
        await SubmitAsync(client, target.Token, 9, email: "race@example.com");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<INpsRepository>();
        // Reproduz a corrida: um contexto que ainda não enxerga a primeira
        // resposta, exatamente o que a segunda requisição simultânea teria.
        dbContext.NpsSurveyResponses.Add(SurveyResponse.Submit(
            new SurveyResponseContext(
                project.Id,
                dispatch.Dispatch.Id,
                target.Id,
                null,
                NpsFormFormat.Simplified,
                NpsDispatchStatus.Open,
                InitialNow.AddDays(10),
                false,
                false,
                false),
            9, null, null, null, null, null, null, "race@example.com", InitialNow));

        var exception = await Assert.ThrowsAnyAsync<Exception>(() => dbContext.SaveChangesAsync());

        Assert.True(repository.IsDuplicateResponseException(exception));
    }

    [Fact]
    public async Task Overdue_indicator_should_ignore_the_response_period()
    {
        var time = new TestTimeProvider(InitialNow);
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, time);
        using var client = factory.CreateClient();
        // Sem disparo e sem resposta: vencido por definição.
        await CreateProjectAsync(client, "Overdue never collected", "Overdue Client", "DC1");
        var answered = await CreateProjectAsync(client, "Overdue answered", "Overdue Client", "DC1");
        var dispatch = await CreateDispatchAsync(client, answered.Id, "simplified");
        await SubmitAsync(client, dispatch.Targets.Single(target => target.IsGeneric).Token, 9);

        var dashboard = await client.GetFromJsonAsync<NpsDashboardView>(
            "/api/nps/dashboard?client=Overdue%20Client&from=2026-08-01&to=2026-08-01");

        Assert.True(
            dashboard!.OverdueProjects >= 1,
            "O indicador de vencidos não pode zerar só porque um período foi escolhido.");
    }

    [Fact]
    public async Task Pending_status_should_survive_a_date_range()
    {
        var time = new TestTimeProvider(InitialNow);
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, time);
        using var client = factory.CreateClient();
        var pending = await CreateProjectAsync(client, "Pending with period", "Pending Client", "DC1");

        var results = await client.GetFromJsonAsync<List<NpsProjectResultView>>(
            "/api/nps/project-results?client=Pending%20Client&status=pending&from=2026-08-01&to=2026-08-31");

        Assert.Equal(pending.Id, Assert.Single(results!).Id);
    }

    [Fact]
    public async Task Responses_should_exclude_waived_projects_unless_asked()
    {
        var time = new TestTimeProvider(InitialNow);
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, time);
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Waived responses", "Waived Client", "DC1");
        var dispatch = await CreateDispatchAsync(client, project.Id, "simplified");
        await SubmitAsync(client, dispatch.Targets.Single(target => target.IsGeneric).Token, 9);
        await client.PostAsJsonAsync($"/api/nps/projects/{project.Id}/waiver", new { reason = "Sem pesquisa" });

        var hidden = await client.GetFromJsonAsync<List<NpsResponseView>>(
            $"/api/nps/responses?projectId={project.Id}");
        var shown = await client.GetFromJsonAsync<List<NpsResponseView>>(
            $"/api/nps/responses?projectId={project.Id}&includeWaived=true");

        Assert.Empty(hidden!);
        Assert.Single(shown!);
    }

    [Fact]
    public async Task Period_boundaries_should_follow_the_timezone_the_timestamps_are_shown_in()
    {
        // 01/09 00:30Z é 31/08 21:30 no horário de operação (UTC-3), e é essa a
        // data que a auditoria exibe: filtrar até 31/08 precisa incluí-la.
        var time = new TestTimeProvider(new DateTimeOffset(2026, 9, 1, 0, 30, 0, TimeSpan.Zero));
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, time);
        using var client = factory.CreateClient();
        var project = await CreateProjectAsync(client, "Timezone boundary", "Timezone Client", "DC1");
        var dispatch = await CreateDispatchAsync(client, project.Id, "simplified");
        await SubmitAsync(client, dispatch.Targets.Single(target => target.IsGeneric).Token, 9);

        var responses = await client.GetFromJsonAsync<List<NpsResponseView>>(
            $"/api/nps/responses?projectId={project.Id}&to=2026-08-31");

        Assert.Single(responses!);
    }

    [Fact]
    public async Task Search_should_treat_a_percent_sign_as_a_literal()
    {
        var time = new TestTimeProvider(InitialNow);
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, time);
        using var client = factory.CreateClient();
        var literal = await CreateProjectAsync(client, "Meta 100% atingida", "Wildcard Client", "DC1");
        await CreateProjectAsync(client, "Meta 1000 pontos", "Wildcard Client", "DC1");

        var results = await client.GetFromJsonAsync<List<NpsProjectResultView>>(
            "/api/nps/project-results?client=Wildcard%20Client&search=100%25");

        Assert.Equal(literal.Id, Assert.Single(results!).Id);
    }

    /// <summary>
    /// Trava a interação entre as duas correções de período: o status sem
    /// resposta convive com a janela (a tabela lista o pendente) e os vencidos
    /// seguem contando fora dela. Os KPIs de resposta ficam zerados porque um
    /// projeto pendente, por definição, não respondeu.
    /// </summary>
    [Fact]
    public async Task Pending_status_with_a_period_should_keep_results_and_overdue_coherent()
    {
        var time = new TestTimeProvider(InitialNow);
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, time);
        using var client = factory.CreateClient();
        var pending = await CreateProjectAsync(client, "Combo pending", "Combo Client", "DC1");
        var answered = await CreateProjectAsync(client, "Combo answered", "Combo Client", "DC1");
        var dispatch = await CreateDispatchAsync(client, answered.Id, "simplified");
        await SubmitAsync(client, dispatch.Targets.Single(target => target.IsGeneric).Token, 9);

        var dashboard = await client.GetFromJsonAsync<NpsDashboardView>(
            "/api/nps/dashboard?client=Combo%20Client&status=pending&from=2026-08-01&to=2026-08-01");
        var results = await client.GetFromJsonAsync<List<NpsProjectResultView>>(
            "/api/nps/project-results?client=Combo%20Client&status=pending&from=2026-08-01&to=2026-08-01");

        Assert.Equal(pending.Id, Assert.Single(results!).Id);
        Assert.Equal(0, dashboard!.TotalResponses);
        Assert.Null(dashboard.OfficialNps);
        Assert.True(dashboard.OverdueProjects >= 1);
    }

    private static async Task<ProjectResponse> CreateProjectAsync(
        HttpClient client,
        string name,
        string clientName = "Client",
        string dc = "DC1")
    {
        var response = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest(
            dc,
            "Em andamento",
            $"{name} {Guid.NewGuid():N}",
            clientName,
            "Squad",
            "2026-01-01",
            "2026-12-31",
            "Maria",
            "None",
            null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectResponse>())!;
    }

    private static async Task<NpsDispatchDetailView> CreateDispatchAsync(
        HttpClient client,
        int projectId,
        string format,
        IReadOnlyList<int>? contactIds = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/nps/dispatches",
            new CreateNpsDispatchRequest(projectId, format, "pt", contactIds ?? []));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NpsDispatchDetailView>())!;
    }

    private static Task<HttpResponseMessage> SubmitAsync(
        HttpClient client,
        Guid token,
        int score,
        string? email = null,
        string? comment = null,
        string? name = null,
        int? quality = null,
        int? schedule = null,
        int? communication = null,
        int? businessValue = null)
        => client.PostAsJsonAsync(
            $"/api/nps/public/{token}/responses",
            new SubmitNpsSurveyResponseRequest(score, quality, schedule, communication, businessValue, comment, name, email));

    private sealed class TestTimeProvider(DateTimeOffset value) : TimeProvider
    {
        private DateTimeOffset _value = value;

        public override DateTimeOffset GetUtcNow() => _value;

        public void Set(DateTimeOffset value) => _value = value;
    }
}
