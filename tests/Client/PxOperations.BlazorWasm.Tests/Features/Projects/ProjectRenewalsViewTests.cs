using Bunit;
using PxOperations.BlazorWasm.Features.Projects;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Projects;

/// <summary>
/// Testes isolados do componente <see cref="ProjectRenewalsView"/>.
/// O componente recebe a lista completa de projetos via parâmetro — não injeta serviços.
/// </summary>
public sealed class ProjectRenewalsViewTests : TestContext
{
    [Fact]
    public void Should_show_coverage_indicator_with_computed_percentage()
    {
        // 2 de 3 projetos com status de renovação preenchido = 66%
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, dc: "DC1", status: "Em andamento", name: "Portal A",
                endDate: "2026-12-31", renewal: "Aprovada"),
            ProjectsTestHelpers.MakeProject(id: 2, dc: "DC1", status: "Em andamento", name: "Portal B",
                endDate: "2026-12-31", renewal: "Pendente"),
            ProjectsTestHelpers.MakeProject(id: 3, dc: "DC1", status: "Em andamento", name: "Portal C",
                endDate: "2026-12-31", renewal: "None")
        };

        var cut = RenderComponent<ProjectRenewalsView>(p => p
            .Add(c => c.Projects, projects.ToList()));

        Assert.Contains("ri-pct", cut.Markup);
        Assert.Contains("66%", cut.Markup);
        Assert.Contains("ri-num aprov", cut.Markup);
    }

    [Fact]
    public void Should_show_dc_bars_when_scope_contains_multiple_dcs()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, dc: "DC1", status: "Em andamento", name: "P1",
                startDate: "2026-01-01", endDate: "2026-06-30", renewal: "Aprovada"),
            ProjectsTestHelpers.MakeProject(id: 2, dc: "DC2", status: "Em andamento", name: "P2",
                startDate: "2026-01-01", endDate: "2026-06-30", renewal: "Pendente")
        };

        var cut = RenderComponent<ProjectRenewalsView>(p => p
            .Add(c => c.Projects, projects.ToList()));

        Assert.Contains("dc-bars-grid", cut.Markup);
        Assert.Contains("dc-bar-card", cut.Markup);
        Assert.Contains("DC1", cut.Markup);
        Assert.Contains("DC2", cut.Markup);
    }

    [Fact]
    public void Should_hide_dc_bars_when_scope_contains_single_dc()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, dc: "DC1", status: "Em andamento", name: "P1",
                endDate: "2026-06-30", renewal: "Aprovada"),
            ProjectsTestHelpers.MakeProject(id: 2, dc: "DC1", status: "Encerrado", name: "P2",
                endDate: "2026-09-30", renewal: "Pendente")
        };

        var cut = RenderComponent<ProjectRenewalsView>(p => p
            .Add(c => c.Projects, projects.ToList()));

        Assert.DoesNotContain("dc-bars-grid", cut.Markup);
    }

    [Fact]
    public void Should_show_project_cards_for_projects_with_renewal_status()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, dc: "DC3", status: "Em andamento",
                name: "Projeto Renovando", client: "Acme",
                startDate: "2026-01-01", endDate: "2026-09-30",
                renewal: "Em andamento", renewalObservation: "Negociação em curso")
        };

        var cut = RenderComponent<ProjectRenewalsView>(p => p
            .Add(c => c.Projects, projects.ToList()));

        Assert.Contains("proj-card highlighted", cut.Markup);
        Assert.Contains("Projeto Renovando", cut.Markup);
        Assert.Contains("Acme", cut.Markup);
        Assert.Contains("Negociação em curso", cut.Markup);
    }

    [Fact]
    public void Should_show_empty_state_when_no_renewals_in_scope()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, dc: "DC1", status: "Em andamento",
                name: "Sem Renov", endDate: "2026-12-31", renewal: "None")
        };

        var cut = RenderComponent<ProjectRenewalsView>(p => p
            .Add(c => c.Projects, projects.ToList()));

        Assert.Contains("Nenhuma renova", cut.Markup);
    }

    [Fact]
    public void Should_include_non_in_progress_statuses_when_provided_by_parent_scope()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, dc: "DC2", status: "Programado",
                name: "Scheduled Renewal", endDate: "2026-12-31", renewal: "Aprovada")
        };

        var cut = RenderComponent<ProjectRenewalsView>(p => p
            .Add(c => c.Projects, projects.ToList()));

        Assert.Contains("Scheduled Renewal", cut.Markup);
        Assert.Contains("100%", cut.Markup);
    }

    [Fact]
    public void Should_fire_on_edit_callback_when_project_card_is_clicked()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 55, dc: "DC1", status: "Em andamento",
                name: "Clicavel", endDate: "2026-12-31", renewal: "Aprovada")
        };

        int? editedId = null;

        var cut = RenderComponent<ProjectRenewalsView>(p => p
            .Add(c => c.Projects, projects.ToList())
            .Add(c => c.OnEdit, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<int>(
                this, id => editedId = id)));

        cut.Find(".proj-card").Click();

        Assert.Equal(55, editedId);
    }
}
