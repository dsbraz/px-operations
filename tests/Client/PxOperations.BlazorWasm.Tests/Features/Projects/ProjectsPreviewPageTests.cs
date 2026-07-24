using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Projects.Preview;
using PxOperations.BlazorWasm.Tests.Helpers;
using PxOperations.Ui.Components.Overlays;

namespace PxOperations.BlazorWasm.Tests.Features.Projects;

public sealed class ProjectsPreviewPageTests : BunitContext
{
    public ProjectsPreviewPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Dashboard_should_focus_on_summary_and_link_to_management()
    {
        RegisterClient(ProjectsTestHelpers.CreateClient(ProjectsTestHelpers.ProjectsJson(
            ProjectsTestHelpers.MakeProject(id: 1, name: "Alpha"))));
        NavigateToDashboard();

        var cut = Render<ProjectsPreviewPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Dashboard de projetos", cut.Find("h1").TextContent.Trim());
            Assert.NotEmpty(cut.FindAll(".px-kpi"));
            Assert.Contains("O que mudou essa semana", cut.Markup);
            Assert.Empty(cut.FindAll("table"));
            Assert.NotEmpty(cut.FindAll("a[href='/preview/projects/manage']"));
        });
    }

    [Fact]
    public void Page_should_reserve_a_named_loading_state_while_api_is_pending()
    {
        RegisterClient(ProjectsTestHelpers.CreateDelayedClient());
        NavigateToManagement();

        var cut = Render<ProjectsPreviewPage>();

        Assert.NotNull(cut.Find("[role='status'][aria-label='Carregando projetos']"));
    }

    [Fact]
    public void Page_should_render_the_complete_projects_surface_with_actions()
    {
        RegisterClient(ProjectsTestHelpers.CreateClient(ProjectsTestHelpers.ProjectsJson(
            ProjectsTestHelpers.MakeProject(id: 1, dc: "DC1", name: "Alpha", client: "CPFL"),
            ProjectsTestHelpers.MakeProject(id: 2, dc: "DC2", name: "Beta", client: "Alelo"))));
        NavigateToManagement();

        var cut = Render<ProjectsPreviewPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("Gestão de projetos", cut.Find("h1").TextContent.Trim());
            Assert.Contains("Alpha", cut.Find("table").TextContent);
            Assert.Contains("Beta", cut.Find("table").TextContent);
            Assert.Equal("Carteira de projetos", cut.Find("caption").TextContent.Trim());
            Assert.Contains("Exportar CSV", cut.Markup);
            Assert.Contains("Novo projeto", cut.Markup);
            Assert.Empty(cut.FindAll(".px-kpi"));
            Assert.DoesNotContain("O que mudou essa semana", cut.Markup);
            Assert.Contains("Lista", cut.Markup);
            Assert.Contains("Kanban", cut.Markup);
            Assert.Contains("Renovações", cut.Markup);
            Assert.Equal(8, cut.FindAll("table .tag.tag--sm").Count);
            Assert.Equal(2, cut.FindAll("table .pill.pill--sm").Count);
            Assert.Empty(cut.FindAll("table .dc-tag, table .tbadge, table .sbadge, table .rbadge, table .dpill"));
            Assert.Empty(cut.FindAll(".table-header"));
        });
    }

    [Fact]
    public void Search_should_filter_name_and_client_and_announce_result_count()
    {
        RegisterClient(ProjectsTestHelpers.CreateClient(ProjectsTestHelpers.ProjectsJson(
            ProjectsTestHelpers.MakeProject(id: 1, name: "Alpha Cloud", client: "CPFL"),
            ProjectsTestHelpers.MakeProject(id: 2, name: "Beta Core", client: "Alelo"))));
        NavigateToManagement();

        var cut = Render<ProjectsPreviewPage>();
        cut.WaitForAssertion(() => Assert.Contains("Alpha Cloud", cut.Markup));

        cut.Find("input[aria-label='Buscar projetos']").Input("CPFL");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Alpha Cloud", cut.Markup);
            Assert.DoesNotContain("Beta Core", cut.Markup);
            Assert.Contains("1 projeto encontrado", cut.Find("[role='status'][data-results-status]").TextContent);
        });
    }

    [Fact]
    public void Filters_should_union_values_inside_a_facet_and_intersect_facets()
    {
        RegisterClient(ProjectsTestHelpers.CreateClient(ProjectsTestHelpers.ProjectsJson(
            ProjectsTestHelpers.MakeProject(id: 1, dc: "DC1", status: "Em andamento", name: "Alpha"),
            ProjectsTestHelpers.MakeProject(id: 2, dc: "DC2", status: "Em andamento", name: "Beta"),
            ProjectsTestHelpers.MakeProject(id: 3, dc: "DC3", status: "Programado", name: "Gamma"))));
        NavigateToManagement();

        var cut = Render<ProjectsPreviewPage>();
        cut.WaitForAssertion(() => Assert.Contains("Gamma", cut.Markup));

        cut.Find("button.fmenu__btn").Click();
        cut.Find("input[type='checkbox'][value='DC1']").Change(true);
        cut.Find("input[type='checkbox'][value='DC2']").Change(true);
        cut.Find("input[type='checkbox'][value='Em andamento']").Change(true);

        cut.WaitForAssertion(() =>
        {
            var tableContent = cut.Find("table").TextContent;
            Assert.Contains("Alpha", tableContent);
            Assert.Contains("Beta", tableContent);
            Assert.DoesNotContain("Gamma", tableContent);
        });
    }

    [Fact]
    public async Task Project_edit_action_should_open_the_project_form()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        RegisterClient(ProjectsTestHelpers.CreateClient(ProjectsTestHelpers.ProjectsJson(
            ProjectsTestHelpers.MakeProject(
                id: 1,
                dc: "DC4",
                status: "Em andamento",
                name: "Alpha",
                client: "CPFL",
                deliveryManager: "Ana Prado"))));
        NavigateToManagement();

        var cut = Render<ProjectsPreviewPage>();
        cut.WaitForAssertion(() => Assert.Contains("Alpha", cut.Markup));

        cut.Find("button[title='Editar']").Click();

        cut.WaitForAssertion(() =>
        {
            var dialog = cut.Find("dialog[aria-modal='true']");
            Assert.Contains("Editar projeto", dialog.TextContent);
            Assert.Contains("Ana Prado", cut.Markup);
        });

        await cut.InvokeAsync(() => cut.FindComponent<BrqDialog>().Instance.NotifyNativeCloseAsync());
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("dialog")));
    }

    [Fact]
    public void Delete_action_should_require_confirmation_before_calling_the_api()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(
            HttpMethod.Get,
            ProjectsTestHelpers.ProjectsJson(
                ProjectsTestHelpers.MakeProject(id: 1, name: "Alpha")),
            HttpStatusCode.OK);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        RegisterClient(httpClient);
        NavigateToManagement();

        var cut = Render<ProjectsPreviewPage>();
        cut.WaitForAssertion(() => Assert.Contains("Alpha", cut.Markup));

        cut.Find("button[title='Remover']").Click();

        cut.WaitForAssertion(() =>
        {
            var dialog = cut.Find("dialog[aria-modal='true']");
            Assert.Contains("Excluir projeto", dialog.TextContent);
            Assert.Contains("Alpha", dialog.TextContent);
            Assert.Single(handler.RequestUris);
        });

        cut.Find(".project-delete-cancel").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("dialog")));
    }

    [Fact]
    public void Failed_request_should_keep_the_preview_usable_with_sample_data()
    {
        RegisterClient(ProjectsTestHelpers.CreateClient(
            """{"title":"Erro"}""",
            HttpStatusCode.InternalServerError));
        NavigateToManagement();

        var cut = Render<ProjectsPreviewPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll("[role='alert']"));
            Assert.Contains("Atlas Portal", cut.Markup);
            Assert.Contains("6 projetos encontrados", cut.Markup);
        });
    }

    private void RegisterClient(HttpClient httpClient)
    {
        Services.AddScoped(_ => httpClient);
        Services.AddScoped<ProjectsClient>();
    }

    private void NavigateToManagement() =>
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .NavigateTo("/preview/projects/manage");

    private void NavigateToDashboard() =>
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .NavigateTo("/preview/projects/dashboard");
}
