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
public sealed class ProjectsPageTests : BunitContext
{
    // ── CARREGAMENTO ─────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_show_loading_while_api_is_pending()
    {
        Services.AddScoped(_ => ProjectsTestHelpers.CreateDelayedClient());
        Services.AddScoped<ProjectsClient>();
        Services.AddScoped<TeamMembersClient>();

        var cut = Render<ProjectsPage>();

        Assert.Equal("Carregando projetos", cut.Find(".brq-loading-state").GetAttribute("aria-label"));
    }

    /// <summary>
    /// A falha de carga não expõe a mensagem técnica ("TypeError: Failed to
    /// fetch") e oferece nova tentativa sem recarregar a página.
    /// </summary>
    [Fact]
    public void Page_should_show_a_friendly_load_error_and_retry()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, "{}", HttpStatusCode.InternalServerError);
        handler.AddResponse(HttpMethod.Get, ProjectsTestHelpers.ProjectsJson(ProjectsTestHelpers.MakeProject(id: 1, name: "Portal Recuperado")), HttpStatusCode.OK);
        Services.AddScoped(_ => new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.AddScoped<ProjectsClient>();
        Services.AddScoped<TeamMembersClient>();

        var cut = Render<ProjectsPage>();

        cut.WaitForAssertion(() =>
            Assert.Contains("Não foi possível carregar os projetos.", cut.Find(".brq-error-state").TextContent));

        cut.Find(".brq-error-state button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".brq-error-state"));
            Assert.Contains("Portal Recuperado", cut.Markup);
        });
    }

    /// <summary>
    /// "/" e "/projects" mostram a mesma página; a URL canônica é
    /// /projects, que é a que o menu reconhece como Projetos.
    /// </summary>
    [Fact]
    public void Root_route_should_redirect_to_projects()
    {
        Services.AddScoped(_ => ProjectsTestHelpers.CreateDelayedClient());
        Services.AddScoped<ProjectsClient>();
        Services.AddScoped<TeamMembersClient>();
        var navigation = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        navigation.NavigateTo("/");

        Render<ProjectsPage>();

        Assert.Equal("http://localhost/projects", navigation.Uri);
    }

    [Fact]
    public void Page_should_render_the_design_system_header_with_its_actions()
    {
        Services.AddScoped(_ => ProjectsTestHelpers.CreateDelayedClient());
        Services.AddScoped<ProjectsClient>();
        Services.AddScoped<TeamMembersClient>();

        var cut = Render<ProjectsPage>();

        Assert.Equal("Projetos", cut.Find("h1.page-head__title").TextContent.Trim());
        Assert.Equal("Carteira de projetos, status e renovações.", cut.Find(".page-head__sub").TextContent.Trim());
        Assert.Equal(
            new[] { "↓ Exportar CSV", "+ Novo Projeto" },
            cut.FindAll(".page-head__actions .brq-button").Select(action => action.TextContent.Trim()));
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
        Services.AddScoped<TeamMembersClient>();

        var cut = Render<ProjectsPage>();

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
        Services.AddScoped<TeamMembersClient>();

        var cut = Render<ProjectsPage>();

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

    [Fact]
    public void Page_should_apply_global_search_filter_to_renewals_tab()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, name: "Alpha Renewal", client: "CPFL",
                endDate: "2026-12-31", renewal: "Aprovada"),
            ProjectsTestHelpers.MakeProject(id: 2, name: "Beta Renewal", client: "Alelo",
                endDate: "2026-12-31", renewal: "Pendente")
        };

        Services.AddScoped(_ => ProjectsTestHelpers.CreateClient(ProjectsTestHelpers.ProjectsJson(projects)));
        Services.AddScoped<ProjectsClient>();
        Services.AddScoped<TeamMembersClient>();

        var cut = Render<ProjectsPage>();

        cut.WaitForAssertion(() => Assert.Contains("Alpha Renewal", cut.Markup));

        cut.FindAll("button[role=tab]")[2].Click();
        cut.Find(".toolbar input[type=text]").Input("Alpha");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Alpha Renewal", cut.Markup);
            Assert.DoesNotContain("Beta Renewal", cut.Markup);
        });
    }

    [Fact]
    public void Page_should_apply_global_dc_filter_to_renewals_tab()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, dc: "DC1", name: "Renewal One",
                endDate: "2026-12-31", renewal: "Aprovada"),
            ProjectsTestHelpers.MakeProject(id: 2, dc: "DC2", name: "Renewal Two",
                endDate: "2026-12-31", renewal: "Pendente")
        };

        Services.AddScoped(_ => ProjectsTestHelpers.CreateClient(ProjectsTestHelpers.ProjectsJson(projects)));
        Services.AddScoped<ProjectsClient>();
        Services.AddScoped<TeamMembersClient>();

        var cut = Render<ProjectsPage>();

        cut.WaitForAssertion(() => Assert.Contains("Renewal One", cut.Markup));

        cut.FindAll("button[role=tab]")[2].Click();
        cut.FindAll(".toolbar select")[0].Change("DC1");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Renewal One", cut.Markup);
            Assert.DoesNotContain("Renewal Two", cut.Markup);
        });
    }

    [Fact]
    public void Page_should_apply_global_status_filter_to_renewals_tab()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, status: "Programado", name: "Scheduled Renewal",
                endDate: "2026-12-31", renewal: "Aprovada"),
            ProjectsTestHelpers.MakeProject(id: 2, status: "Em andamento", name: "In Progress Renewal",
                endDate: "2026-12-31", renewal: "Pendente")
        };

        Services.AddScoped(_ => ProjectsTestHelpers.CreateClient(ProjectsTestHelpers.ProjectsJson(projects)));
        Services.AddScoped<ProjectsClient>();
        Services.AddScoped<TeamMembersClient>();

        var cut = Render<ProjectsPage>();

        cut.WaitForAssertion(() => Assert.Contains("Scheduled Renewal", cut.Markup));

        cut.FindAll("button[role=tab]")[2].Click();
        cut.FindAll(".toolbar select")[1].Change("Programado");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Scheduled Renewal", cut.Markup);
            Assert.DoesNotContain("In Progress Renewal", cut.Markup);
        });
    }

    [Fact]
    public void Page_should_apply_global_renewal_filter_to_renewals_tab()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, name: "Approved Renewal",
                endDate: "2026-12-31", renewal: "Aprovada"),
            ProjectsTestHelpers.MakeProject(id: 2, name: "Pending Renewal",
                endDate: "2026-12-31", renewal: "Pendente"),
            ProjectsTestHelpers.MakeProject(id: 3, name: "No Renewal",
                endDate: "2026-12-31", renewal: "None")
        };

        Services.AddScoped(_ => ProjectsTestHelpers.CreateClient(ProjectsTestHelpers.ProjectsJson(projects)));
        Services.AddScoped<ProjectsClient>();
        Services.AddScoped<TeamMembersClient>();

        var cut = Render<ProjectsPage>();

        cut.WaitForAssertion(() => Assert.Contains("Approved Renewal", cut.Markup));

        cut.FindAll("button[role=tab]")[2].Click();
        cut.FindAll(".toolbar select")[3].Change("Aprovada");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Approved Renewal", cut.Markup);
            Assert.DoesNotContain("Pending Renewal", cut.Markup);
            Assert.DoesNotContain("No Renewal", cut.Markup);
        });
    }

    // ── MODAL ────────────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_show_create_modal_when_new_project_button_is_clicked()
    {
        Services.AddScoped(_ => ProjectsTestHelpers.CreateEmptyClient());
        Services.AddScoped<ProjectsClient>();
        Services.AddScoped<TeamMembersClient>();

        var cut = Render<ProjectsPage>();

        cut.WaitForAssertion(() => Assert.DoesNotContain("overlay open", cut.Markup));

        cut.Find(".page-head__actions .brq-button--primary").Click();

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
        Services.AddScoped<TeamMembersClient>();

        var cut = Render<ProjectsPage>();

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
        Services.AddScoped<TeamMembersClient>();

        var cut = Render<ProjectsPage>();

        cut.Find(".page-head__actions .brq-button--primary").Click();
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
        Services.AddScoped<TeamMembersClient>();

        var cut = Render<ProjectsPage>();

        cut.WaitForAssertion(() => Assert.Contains("ToDelete", cut.Markup));

        cut.Find("button.ibtn.del").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("ToDelete", cut.Markup));
    }

    /// <summary>
    /// Uma exclusão que falha não é falha de carga: a lista continua na tela
    /// e o aviso diz que a ação não foi concluída, sem "Tentar novamente".
    /// </summary>
    [Fact]
    public void Failed_delete_should_keep_the_list_and_report_the_action()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, ProjectsTestHelpers.ProjectsJson(
            ProjectsTestHelpers.MakeProject(id: 1, name: "Portal Mantido")),
            HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Delete, "{}", HttpStatusCode.InternalServerError);
        Services.AddScoped(_ => new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.AddScoped<ProjectsClient>();
        Services.AddScoped<TeamMembersClient>();

        var cut = Render<ProjectsPage>();
        cut.WaitForAssertion(() => Assert.Contains("Portal Mantido", cut.Markup));

        cut.Find("button.ibtn.del").Click();

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find(".brq-error-state");
            Assert.Equal("Não foi possível concluir a ação", alert.QuerySelector("h2")!.TextContent.Trim());
            Assert.Equal("Não foi possível excluir o projeto.", alert.QuerySelector("p")!.TextContent.Trim());
            Assert.Null(alert.QuerySelector("button"));
            Assert.Contains("Portal Mantido", cut.Markup);
        });
    }
}
