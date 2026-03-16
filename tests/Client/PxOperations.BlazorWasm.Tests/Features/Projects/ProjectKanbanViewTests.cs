using System.Net;
using System.Net.Http;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Projects;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Projects;

/// <summary>
/// Testes isolados do componente <see cref="ProjectKanbanView"/>.
/// A maioria dos testes não precisa de HTTP; apenas o drag-drop usa <see cref="ProjectsClient"/>.
/// </summary>
public sealed class ProjectKanbanViewTests : TestContext
{
    // Registra um ProjectsClient de stub sem operações — suficiente para a maioria dos testes.
    private void RegisterEmptyClient()
    {
        Services.AddScoped(_ => ProjectsTestHelpers.CreateEmptyClient());
        Services.AddScoped<ProjectsClient>();
    }

    [Fact]
    public void Should_render_board_grouped_by_status_by_default()
    {
        RegisterEmptyClient();

        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, status: "Em andamento", name: "Projeto Ativo"),
            ProjectsTestHelpers.MakeProject(id: 2, status: "Programado",   name: "Projeto Futuro"),
            ProjectsTestHelpers.MakeProject(id: 3, status: "Encerrado",    name: "Projeto Velho")
        };

        var cut = RenderComponent<ProjectKanbanView>(p => p
            .Add(c => c.Projects, projects.ToList()));

        Assert.Contains("kanban-board", cut.Markup);
        Assert.Equal(3, cut.FindAll(".kanban-column").Count);
        Assert.Contains("Programado", cut.Markup);
        Assert.Contains("Em andamento", cut.Markup);
        Assert.Contains("Encerrado", cut.Markup);
    }

    [Fact]
    public void Should_show_full_card_details()
    {
        RegisterEmptyClient();

        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(
                id: 1, dc: "DC3", status: "Em andamento", name: "Portal X",
                client: "Acme", type: "Escopo Fechado", deliveryManager: "Ana Souza",
                renewal: "Pendente")
        };

        var cut = RenderComponent<ProjectKanbanView>(p => p
            .Add(c => c.Projects, projects.ToList()));

        Assert.Contains("kanban-card", cut.Markup);
        Assert.Contains("Portal X", cut.Markup);
        Assert.Contains("DC3", cut.Markup);
        Assert.Contains("Acme", cut.Markup);
        Assert.Contains("Escopo Fechado", cut.Markup);
        Assert.Contains("Ana Souza", cut.Markup);
    }

    [Fact]
    public void Should_group_by_renewal_showing_four_columns()
    {
        RegisterEmptyClient();

        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, name: "P1", renewal: "Aprovada"),
            ProjectsTestHelpers.MakeProject(id: 2, name: "P2", renewal: "Pendente")
        };

        var cut = RenderComponent<ProjectKanbanView>(p => p
            .Add(c => c.Projects, projects.ToList()));

        cut.FindAll("button.kgtab")[1].Click(); // 0=Status, 1=Renovação, 2=DC

        Assert.Equal(4, cut.FindAll(".kanban-column").Count);
        Assert.Contains("Aprovada", cut.Markup);
        Assert.Contains("Pendente", cut.Markup);
        Assert.Contains("Sem status", cut.Markup);
    }

    [Fact]
    public void Should_group_by_dc_showing_six_columns()
    {
        RegisterEmptyClient();

        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, dc: "DC1", name: "P1"),
            ProjectsTestHelpers.MakeProject(id: 2, dc: "DC3", name: "P2")
        };

        var cut = RenderComponent<ProjectKanbanView>(p => p
            .Add(c => c.Projects, projects.ToList()));

        cut.FindAll("button.kgtab")[2].Click(); // 0=Status, 1=Renovação, 2=DC

        Assert.Equal(6, cut.FindAll(".kanban-column").Count);
    }

    [Fact]
    public void Should_render_only_projects_in_the_provided_list()
    {
        // O filtro é responsabilidade de ProjectsPage; o Kanban apenas exibe o que recebe.
        RegisterEmptyClient();

        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, name: "Alpha Squad"),
        };

        var cut = RenderComponent<ProjectKanbanView>(p => p
            .Add(c => c.Projects, projects.ToList()));

        Assert.Contains("Alpha Squad", cut.Markup);
        Assert.DoesNotContain("Beta Squad", cut.Markup);
    }

    [Fact]
    public void Should_fire_on_edit_callback_when_card_is_clicked()
    {
        RegisterEmptyClient();

        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 99, name: "Portal Editavel")
        };

        int? editedId = null;

        var cut = RenderComponent<ProjectKanbanView>(p => p
            .Add(c => c.Projects, projects.ToList())
            .Add(c => c.OnEdit, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<int>(
                this, id => editedId = id)));

        cut.Find(".kanban-card").Click();

        Assert.Equal(99, editedId);
    }

    [Fact]
    public void Should_show_empty_message_in_columns_with_no_cards()
    {
        RegisterEmptyClient();

        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, status: "Em andamento", name: "Unico")
        };

        var cut = RenderComponent<ProjectKanbanView>(p => p
            .Add(c => c.Projects, projects.ToList()));

        // Colunas "Programado" e "Encerrado" devem mostrar mensagem vazia
        Assert.Contains("Nenhum projeto", cut.Markup);
    }

    [Fact]
    public void Should_fire_on_toast_after_successful_drag_drop()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Patch, ProjectsTestHelpers.ProjectJson(
            ProjectsTestHelpers.MakeProject(id: 1, status: "Em andamento", name: "Mover Projeto")),
            HttpStatusCode.OK);

        Services.AddScoped(_ => new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.AddScoped<ProjectsClient>();

        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, status: "Programado", name: "Mover Projeto")
        };

        string? toastMessage = null;

        var cut = RenderComponent<ProjectKanbanView>(p => p
            .Add(c => c.Projects, projects.ToList())
            .Add(c => c.OnToast, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<string>(
                this, msg => toastMessage = msg)));

        cut.Find(".kanban-card").TriggerEvent("ondragstart", new DragEventArgs());
        cut.FindAll(".kanban-col-body")[1].TriggerEvent("ondrop", new DragEventArgs());

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(toastMessage);
            Assert.Contains("Projeto movido", toastMessage);
        });
    }
}
