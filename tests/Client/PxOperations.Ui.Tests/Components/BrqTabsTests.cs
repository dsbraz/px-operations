using Bunit;
using PxOperations.Ui.Components.Navigation;

namespace PxOperations.Ui.Tests.Components;

/// <summary>
/// Abas de visão no contrato do painel de NPS: sublinhado no item ativo,
/// botão quando a visão é estado da página e link quando ela tem rota própria.
/// </summary>
public sealed class BrqTabsTests : BunitContext
{
    [Fact]
    public void Should_mark_only_the_selected_tab_and_report_the_chosen_key()
    {
        string? chosen = null;
        var cut = Render<BrqTabs>(parameters => parameters
            .Add(component => component.AccessibleName, "Visões de projetos")
            .Add(component => component.Items, new[]
            {
                new BrqTabItem("lista", "Lista"),
                new BrqTabItem("kanban", "Kanban")
            })
            .Add(component => component.SelectedKey, "lista")
            .Add(component => component.OnSelect, key => chosen = key));

        Assert.Equal("Visões de projetos", cut.Find("[role=tablist]").GetAttribute("aria-label"));
        var tabs = cut.FindAll("button.tab");
        Assert.Equal(new[] { "true", "false" }, tabs.Select(tab => tab.GetAttribute("aria-selected")));

        tabs[1].Click();

        Assert.Equal("kanban", chosen);
    }

    [Fact]
    public void Should_render_links_for_tabs_with_their_own_route()
    {
        string? chosen = null;
        var cut = Render<BrqTabs>(parameters => parameters
            .Add(component => component.Items, new[]
            {
                new BrqTabItem("coleta", "Coleta", "/nps/coleta"),
                new BrqTabItem("resultados", "Resultados", "/nps/resultados")
            })
            .Add(component => component.SelectedKey, "resultados")
            .Add(component => component.OnSelect, key => chosen = key));

        var links = cut.FindAll("a.tab");
        Assert.Equal(new[] { "/nps/coleta", "/nps/resultados" }, links.Select(link => link.GetAttribute("href")));
        Assert.Equal("true", links[1].GetAttribute("aria-selected"));

        links[0].Click();

        Assert.Equal("coleta", chosen);
    }
}
