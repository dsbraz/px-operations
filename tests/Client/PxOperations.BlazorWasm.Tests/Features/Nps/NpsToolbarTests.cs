using Bunit;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

public sealed class NpsToolbarTests : BunitContext
{
    public NpsToolbarTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Toolbar_should_mark_the_active_tab_and_link_the_others()
    {
        var cut = Render<NpsToolbar>(parameters => parameters
            .Add(toolbar => toolbar.ActiveTab, NpsTab.Results)
            .Add(toolbar => toolbar.CollectionHref, "/nps/coleta?client=Alpha")
            .Add(toolbar => toolbar.ResultsHref, "/nps/resultados?client=Alpha")
            .Add(toolbar => toolbar.ResponsesHref, "/nps/respostas?client=Alpha"));

        var active = cut.Find("a.vtab.active");
        Assert.Equal("Resultados", active.TextContent.Trim());
        Assert.Equal("/nps/coleta?client=Alpha", cut.FindAll("a.vtab")[0].GetAttribute("href"));
    }

    /// <summary>
    /// As facetas comparam sem diferenciar maiúsculas. Se o conjunto chegasse
    /// por uma interface, o Contains cairia na sobrecarga do LINQ e a marcação
    /// passaria a depender da caixa do código vindo do servidor.
    /// </summary>
    [Fact]
    public void Checked_facet_should_ignore_the_case_of_the_server_code()
    {
        var cut = Render<NpsToolbar>(parameters => parameters
            .Add(toolbar => toolbar.ShowsFilters, true)
            .Add(toolbar => toolbar.ActiveTab, NpsTab.Collection)
            .Add(toolbar => toolbar.Clients, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "alpha" })
            .Add(toolbar => toolbar.FilterOptions, new NpsFilterOptionsView
            {
                Clients = [new NpsOptionView { Code = "Alpha", Label = "Alpha" }],
                Dcs = [], ProjectTypes = [], DeliveryManagers = [],
                Statuses = [], Formats = [], Classifications = []
            }));

        cut.Find("button.fmenu__btn").Click();

        Assert.True(cut.Find(".fmenu__pop input[type=checkbox]").HasAttribute("checked"));
    }

    [Fact]
    public async Task Toggling_a_facet_should_report_the_key_and_the_value()
    {
        NpsFacetToggle? reported = null;
        var cut = Render<NpsToolbar>(parameters => parameters
            .Add(toolbar => toolbar.ShowsFilters, true)
            .Add(toolbar => toolbar.ActiveTab, NpsTab.Collection)
            .Add(toolbar => toolbar.FilterOptions, new NpsFilterOptionsView
            {
                Clients = [new NpsOptionView { Code = "Alpha", Label = "Alpha" }],
                Dcs = [], ProjectTypes = [], DeliveryManagers = [],
                Statuses = [], Formats = [], Classifications = []
            })
            .Add(toolbar => toolbar.OnToggleFacet, toggle => reported = toggle));

        cut.Find("button.fmenu__btn").Click();
        await cut.Find(".fmenu__pop input[type=checkbox]").ChangeAsync(new() { Value = true });

        Assert.Equal(new NpsFacetToggle("client", "Alpha"), reported);
    }
}
