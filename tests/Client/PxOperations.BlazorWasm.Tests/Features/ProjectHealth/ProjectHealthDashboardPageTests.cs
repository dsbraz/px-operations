using System.Net;
using System.Net.Http;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.ProjectHealth;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.ProjectHealth;

public sealed class ProjectHealthDashboardPageTests : TestContext
{
    [Fact]
    public void Page_should_show_loading_while_api_is_pending()
    {
        Services.AddScoped(_ => ProjectsTestHelpers.CreateDelayedClient());
        Services.AddScoped<ProjectHealthClient>();

        var cut = RenderComponent<ProjectHealthDashboardPage>();

        Assert.Contains("Carregando", cut.Markup);
    }

    [Fact]
    public void Page_should_render_stats_bar_with_summary_data()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.SummaryJson(
            totalEntries: 5, totalProjects: 3, avgScore: 7.5, criticalCount: 1, noResponseCount: 2
        ), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.ProjectHealthListJson(
            ProjectHealthTestHelpers.MakeProjectHealth(1, score: 10, highlights: "Tudo ótimo")
        ), HttpStatusCode.OK);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<ProjectHealthClient>();

        var cut = RenderComponent<ProjectHealthDashboardPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Saúde de Projetos", cut.Markup);
            Assert.Contains("3", cut.Markup);
            Assert.Contains("7.5", cut.Markup);
        });
    }

    [Fact]
    public void Page_should_switch_between_dashboard_and_projects_tabs()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.SummaryJson(totalProjects: 1), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.ProjectHealthListJson(
            ProjectHealthTestHelpers.MakeProjectHealth(1, highlights: "Destaque da semana")
        ), HttpStatusCode.OK);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<ProjectHealthClient>();

        var cut = RenderComponent<ProjectHealthDashboardPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Saúde Geral", cut.Markup);
        });

        var projetosTab = cut.FindAll(".vtab")[1];
        projetosTab.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Projeto Teste", cut.Markup);
        });
    }

    [Fact]
    public void Toolbar_should_render_period_options_from_weekly_evolution()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.SummaryJson(
            totalProjects: 3, weeks: ["2026-03-23", "2026-03-30"]
        ), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "[]", HttpStatusCode.OK);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<ProjectHealthClient>();

        var cut = RenderComponent<ProjectHealthDashboardPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Última semana", cut.Markup);
            var options = cut.FindAll("option");
            Assert.Contains(options, o => o.GetAttribute("value") == "2026-03-30");
            Assert.Contains(options, o => o.GetAttribute("value") == "2026-03-23");
        });
    }

    [Fact]
    public void Selecting_a_period_reloads_both_summary_and_list()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        // Initial load
        handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.SummaryJson(
            totalProjects: 3, avgScore: 7.5, weeks: ["2026-03-23", "2026-03-30"]
        ), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.ProjectHealthListJson(
            ProjectHealthTestHelpers.MakeProjectHealth(1, highlights: "Antes")
        ), HttpStatusCode.OK);
        // After selecting an older period: new summary + new list
        handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.SummaryJson(
            totalProjects: 1, avgScore: 2.0, weeks: ["2026-03-23", "2026-03-30"]
        ), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.ProjectHealthListJson(
            ProjectHealthTestHelpers.MakeProjectHealth(2, projectName: "Projeto Antigo", highlights: "Depois")
        ), HttpStatusCode.OK);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<ProjectHealthClient>();

        var cut = RenderComponent<ProjectHealthDashboardPage>();
        cut.WaitForAssertion(() => Assert.Contains("7.5", cut.Markup));

        // Period select is the second dropdown (DC, Período, Nota).
        cut.FindAll("select")[1].Change("2026-03-23");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("2.0", cut.Markup);            // summary refetched
            Assert.DoesNotContain("7.5", cut.Markup);
        });
    }

    [Fact]
    public void Changing_score_filter_reloads_only_the_list_not_the_summary()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        // Initial load: summary + list.
        handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.SummaryJson(
            totalProjects: 3, avgScore: 7.5
        ), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.ProjectHealthListJson(
            ProjectHealthTestHelpers.MakeProjectHealth(1, highlights: "Antes")
        ), HttpStatusCode.OK);
        // Only ONE further response (list). If the score change wrongly refetched the summary,
        // this list JSON would be consumed as the summary response and deserialization would fail.
        handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.ProjectHealthListJson(
            ProjectHealthTestHelpers.MakeProjectHealth(2, projectName: "Filtrado", highlights: "Depois")
        ), HttpStatusCode.OK);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<ProjectHealthClient>();

        var cut = RenderComponent<ProjectHealthDashboardPage>();
        cut.WaitForAssertion(() => Assert.Contains("7.5", cut.Markup));

        // Nota is the third dropdown (DC, Período, Nota).
        cut.FindAll("select")[2].Change("lo");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Depois", cut.Markup);   // list reloaded
            Assert.Contains("7.5", cut.Markup);       // summary preserved (not refetched/cleared)
            Assert.DoesNotContain("Não foi possível carregar", cut.Markup);
        });
    }

    [Fact]
    public void Changing_dc_resets_the_period_filter_to_latest_week()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        // Three full reloads (initial, after week select, after DC change), each summary + list.
        for (var i = 0; i < 3; i++)
        {
            handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.SummaryJson(
                totalProjects: 2, weeks: ["2026-03-23", "2026-03-30"]
            ), HttpStatusCode.OK);
            handler.AddResponse(HttpMethod.Get, "[]", HttpStatusCode.OK);
        }

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<ProjectHealthClient>();

        var cut = RenderComponent<ProjectHealthDashboardPage>();
        cut.WaitForAssertion(() => Assert.Contains("Última semana", cut.Markup));

        cut.FindAll("select")[1].Change("2026-03-23"); // pick an explicit period
        cut.WaitForAssertion(() => Assert.Equal("2026-03-23", cut.FindAll("select")[1].GetAttribute("value")));

        cut.FindAll("select")[0].Change("DC2");        // change DC
        cut.WaitForAssertion(() => Assert.Equal("", cut.FindAll("select")[1].GetAttribute("value"))); // week reset
    }

    [Fact]
    public void Detail_modal_should_render_action_plan_comment()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.SummaryJson(totalProjects: 1), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.ProjectHealthListJson(
            ProjectHealthTestHelpers.MakeProjectHealth(1, actionPlanNeeded: true,
                actionPlanComment: "Necessário plano para mitigação de riscos.", highlights: "Destaque")
        ), HttpStatusCode.OK);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<ProjectHealthClient>();

        var cut = RenderComponent<ProjectHealthDashboardPage>();
        cut.WaitForAssertion(() => Assert.Contains("Destaque", cut.Markup));

        cut.Find(".ph-com-card").Click(); // open the detail modal

        cut.WaitForAssertion(() =>
            Assert.Contains("Necessário plano para mitigação de riscos.", cut.Markup));
    }

    [Fact]
    public void Page_should_render_link_to_form()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, ProjectHealthTestHelpers.SummaryJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "[]", HttpStatusCode.OK);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<ProjectHealthClient>();

        var cut = RenderComponent<ProjectHealthDashboardPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("/project-health/new", cut.Markup);
            Assert.Contains("Formulário do Líder", cut.Markup);
        });
    }
}
