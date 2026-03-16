using Bunit;
using PxOperations.BlazorWasm.Features.Projects;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Projects;

/// <summary>
/// Testes isolados do componente <see cref="WeeklyPulse"/>.
/// O componente não injeta serviços — recebe apenas a lista de projetos via parâmetro.
/// </summary>
public sealed class WeeklyPulseTests : TestContext
{
    [Fact]
    public void Should_render_four_cards_with_labels()
    {
        var cut = RenderComponent<WeeklyPulse>(p => p
            .Add(c => c.Projects, []));

        Assert.Contains("Novos programados", cut.Markup);
        Assert.Contains("Iniciados semana ant.", cut.Markup);
        Assert.Contains("Encerrados semana ant.", cut.Markup);
        Assert.Contains("Renova", cut.Markup); // "Renovações aprovadas"
    }

    [Fact]
    public void Should_count_programmed_projects_in_new_card()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, status: "Programado", name: "Novo Projeto"),
            ProjectsTestHelpers.MakeProject(id: 2, status: "Em andamento", name: "Ativo")
        };

        var cut = RenderComponent<WeeklyPulse>(p => p
            .Add(c => c.Projects, projects.ToList()));

        var counts = cut.FindAll(".pc-new .pc-count");
        Assert.Single(counts);
        Assert.Equal("1", counts[0].TextContent.Trim());
    }

    [Fact]
    public void Should_count_approved_renewals()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, name: "Portal X", renewal: "Aprovada"),
            ProjectsTestHelpers.MakeProject(id: 2, name: "Portal Y", renewal: "Pendente")
        };

        var cut = RenderComponent<WeeklyPulse>(p => p
            .Add(c => c.Projects, projects.ToList()));

        var counts = cut.FindAll(".pc-renew .pc-count");
        Assert.Single(counts);
        Assert.Equal("1", counts[0].TextContent.Trim());
    }

    [Fact]
    public void Should_show_project_name_in_card_items()
    {
        var projects = new[]
        {
            ProjectsTestHelpers.MakeProject(id: 1, status: "Programado", name: "Meu Projeto Novo")
        };

        var cut = RenderComponent<WeeklyPulse>(p => p
            .Add(c => c.Projects, projects.ToList()));

        Assert.Contains("Meu Projeto Novo", cut.Markup);
    }

    [Fact]
    public void Should_collapse_and_expand_on_header_click()
    {
        var cut = RenderComponent<WeeklyPulse>(p => p
            .Add(c => c.Projects, []));

        Assert.Contains("pulse-body open", cut.Markup);

        cut.Find(".pulse-header").Click();

        Assert.DoesNotContain("pulse-body open", cut.Markup);
        Assert.Contains("pulse-header collapsed", cut.Markup);

        cut.Find(".pulse-header").Click();

        Assert.Contains("pulse-body open", cut.Markup);
    }

    [Fact]
    public void Should_show_empty_message_when_no_projects_in_category()
    {
        var cut = RenderComponent<WeeklyPulse>(p => p
            .Add(c => c.Projects, []));

        Assert.Contains("Nenhum esta semana", cut.Markup);
        Assert.Contains("Nenhuma aprovada", cut.Markup);
    }
}
