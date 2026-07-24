using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.Ui.Components.DataDisplay;
using PxOperations.Ui.Components.Navigation;
using PxOperations.Ui.Theming;

namespace PxOperations.Ui.Tests.Components;

public sealed class NpsVisualContractTests : BunitContext
{
    public NpsVisualContractTests()
    {
        Services.AddSingleton<IThemeService>(new FakeThemeService());
    }

    [Fact]
    public void Kpi_components_should_use_the_nps_dashboard_card_contract()
    {
        var cut = Render<BrqKpiBar>(parameters => parameters
            .AddChildContent<BrqKpiCard>(card => card
                .Add(component => component.Label, "Projetos")
                .Add(component => component.Value, "19")
                .Add(component => component.Context, "carteira completa")));

        Assert.NotNull(cut.Find("section.kpi-grid"));
        Assert.NotNull(cut.Find("article.kpi"));
        Assert.Equal("Projetos", cut.Find(".kpi__label").TextContent);
        Assert.Equal("19", cut.Find(".kpi__value").TextContent);
        Assert.Equal("carteira completa", cut.Find(".kpi__foot").TextContent);
        Assert.Empty(cut.FindAll(".brq-kpi-icon"));
    }

    [Fact]
    public void Page_header_should_use_the_compact_nps_dashboard_hierarchy()
    {
        var cut = Render<BrqPageHeader>(parameters => parameters
            .Add(component => component.Title, "Projetos")
            .Add(component => component.Description, "Carteira ativa"));

        Assert.NotNull(cut.Find("header.page-head"));
        Assert.NotNull(cut.Find("h1.page-head__title"));
        Assert.NotNull(cut.Find("p.page-head__sub"));
    }

    private sealed class FakeThemeService : IThemeService
    {
        public ThemePreference Current => ThemePreference.Light;

        public event Action? Changed;

        public ValueTask InitializeAsync() => ValueTask.CompletedTask;

        public ValueTask ToggleAsync()
        {
            Changed?.Invoke();
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
