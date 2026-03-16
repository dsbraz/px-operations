using Bunit;
using PxOperations.BlazorWasm.Features.Projects;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Projects;

/// <summary>
/// Testes isolados do componente <see cref="ProjectListView"/>.
/// O componente recebe projetos e flags via parâmetros — não injeta serviços.
/// </summary>
public sealed class ProjectListViewTests : TestContext
{
    [Fact]
    public void Should_render_table_with_projects()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, dc: "DC1", name: "Alpha", client: "Client A")
        };

        var cut = RenderComponent<ProjectListView>(p => p
            .Add(c => c.Projects, projects.ToList())
            .Add(c => c.IsLoading, false));

        Assert.Contains("Alpha", cut.Markup);
        Assert.Contains("DC1", cut.Markup);
        Assert.Contains("Client A", cut.Markup);
    }

    [Fact]
    public void Should_render_empty_state_when_no_projects()
    {
        var cut = RenderComponent<ProjectListView>(p => p
            .Add(c => c.Projects, [])
            .Add(c => c.IsLoading, false));

        Assert.Contains("Nenhum projeto encontrado", cut.Markup);
    }

    [Fact]
    public void Should_show_loading_card_when_is_loading_is_true()
    {
        var cut = RenderComponent<ProjectListView>(p => p
            .Add(c => c.Projects, [])
            .Add(c => c.IsLoading, true));

        Assert.Contains("Carregando", cut.Markup);
    }

    [Fact]
    public void Should_show_error_card_when_error_message_is_set()
    {
        var cut = RenderComponent<ProjectListView>(p => p
            .Add(c => c.Projects, [])
            .Add(c => c.IsLoading, false)
            .Add(c => c.ErrorMessage, "Erro ao carregar projetos"));

        Assert.Contains("Erro ao carregar projetos", cut.Markup);
    }

    [Fact]
    public void Should_collapse_and_expand_on_minimize_button_click()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, name: "Alpha")
        };

        var cut = RenderComponent<ProjectListView>(p => p
            .Add(c => c.Projects, projects.ToList())
            .Add(c => c.IsLoading, false));

        Assert.Contains("table-wrap open", cut.Markup);

        cut.Find("button.collapse-btn").Click();

        Assert.DoesNotContain("table-wrap open", cut.Markup);
        Assert.Contains("Expandir lista", cut.Markup);

        cut.Find("button.collapse-btn").Click();

        Assert.Contains("table-wrap open", cut.Markup);
        Assert.Contains("Minimizar lista", cut.Markup);
    }

    [Fact]
    public void Should_fire_on_edit_callback_when_edit_button_clicked()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 42, name: "Portal X")
        };

        int? editedId = null;

        var cut = RenderComponent<ProjectListView>(p => p
            .Add(c => c.Projects, projects.ToList())
            .Add(c => c.IsLoading, false)
            .Add(c => c.OnEdit, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<int>(
                this, id => editedId = id)));

        cut.Find("button.ibtn:not(.del)").Click();

        Assert.Equal(42, editedId);
    }

    [Fact]
    public void Should_fire_on_delete_callback_when_delete_button_clicked()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 7, name: "To Delete")
        };

        int? deletedId = null;

        var cut = RenderComponent<ProjectListView>(p => p
            .Add(c => c.Projects, projects.ToList())
            .Add(c => c.IsLoading, false)
            .Add(c => c.OnDelete, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<int>(
                this, id => deletedId = id)));

        cut.Find("button.ibtn.del").Click();

        Assert.Equal(7, deletedId);
    }
}
