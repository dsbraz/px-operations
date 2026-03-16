using System.Net;
using System.Net.Http;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Projects;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Projects;

/// <summary>
/// Testes de integração da página <see cref="ProjectsPage"/>.
/// Cobrem a orquestração entre a página e seus subcomponentes:
/// carregamento via API, barra de estatísticas, filtro de busca,
/// abertura/fechamento do modal e exclusão de projetos.
/// Comportamentos internos de cada subcomponente são testados nos
/// arquivos de teste correspondentes.
/// </summary>
public sealed class ProjectsPageTests : TestContext
{
    // ── CARREGAMENTO ─────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_show_loading_while_api_is_pending()
    {
        Services.AddScoped(_ => ProjectsTestHelpers.CreateDelayedClient());
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<ProjectsPage>();

        Assert.Contains("Carregando", cut.Markup);
    }

    // ── STATS BAR ────────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_render_stats_bar_with_aggregated_counts()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, status: "Em andamento", name: "Alpha", client: "CPFL"),
            ProjectsTestHelpers.MakeProject(id: 2, status: "Encerrado",    name: "Beta",  client: "CPFL",
                renewal: "Aprovada")
        };

        Services.AddScoped(_ => ProjectsTestHelpers.CreateClient(ProjectsTestHelpers.ProjectsJson(projects)));
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<ProjectsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Projetos", cut.Markup);
            Assert.Contains("Em andamento", cut.Markup);
        });
    }

    // ── FILTRO DE BUSCA ──────────────────────────────────────────────────────

    [Fact]
    public void Page_should_filter_subcomponents_by_search_term()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, name: "Alpha Project", client: "CPFL"),
            ProjectsTestHelpers.MakeProject(id: 2, name: "Beta Project",  client: "Alelo")
        };

        Services.AddScoped(_ => ProjectsTestHelpers.CreateClient(ProjectsTestHelpers.ProjectsJson(projects)));
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<ProjectsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Alpha Project", cut.Markup);
            Assert.Contains("Beta Project", cut.Markup);
        });

        cut.Find("input[type=text]").Input("Alpha");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Alpha Project", cut.Markup);
            Assert.DoesNotContain("Beta Project", cut.Markup);
        });
    }

    // ── MODAL ────────────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_show_create_modal_when_new_project_button_is_clicked()
    {
        Services.AddScoped(_ => ProjectsTestHelpers.CreateEmptyClient());
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<ProjectsPage>();

        cut.WaitForAssertion(() => Assert.DoesNotContain("overlay open", cut.Markup));

        cut.Find("button.btn-purple").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Novo Projeto", cut.Markup);
            Assert.Contains("overlay open", cut.Markup);
        });
    }

    [Fact]
    public void Page_should_open_edit_modal_with_project_data_when_row_edit_is_clicked()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, dc: "DC2", status: "Programado", name: "Portal X",
                client: "Acme", type: "Escopo Fechado", startDate: "2026-03-01", endDate: "2026-09-01",
                deliveryManager: "Flavia de Castro", renewal: "Pendente", renewalObservation: "Obs test")
        };

        Services.AddScoped(_ => ProjectsTestHelpers.CreateClient(ProjectsTestHelpers.ProjectsJson(projects)));
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<ProjectsPage>();

        cut.WaitForAssertion(() => Assert.Contains("Portal X", cut.Markup));

        cut.Find("button.ibtn:not(.del)").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Editar Projeto", cut.Markup);
            Assert.Contains("overlay open", cut.Markup);
            Assert.Contains("Portal X", cut.Markup);
        });
    }

    [Fact]
    public void Page_should_close_modal_when_cancel_is_clicked()
    {
        Services.AddScoped(_ => ProjectsTestHelpers.CreateEmptyClient());
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<ProjectsPage>();

        cut.Find("button.btn-purple").Click();
        cut.WaitForAssertion(() => Assert.Contains("overlay open", cut.Markup));

        cut.Find(".mfoot .btn-ghost").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("overlay open", cut.Markup));
    }

    // ── EXCLUSÃO ─────────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_remove_project_from_list_after_delete()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, ProjectsTestHelpers.ProjectsJson(
            ProjectsTestHelpers.MakeProject(id: 1, name: "ToDelete")),
            HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Delete, "", HttpStatusCode.NoContent);

        Services.AddScoped(_ => new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<ProjectsPage>();

        cut.WaitForAssertion(() => Assert.Contains("ToDelete", cut.Markup));

        cut.Find("button.ibtn.del").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("ToDelete", cut.Markup));
    }
}
