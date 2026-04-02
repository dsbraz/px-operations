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
