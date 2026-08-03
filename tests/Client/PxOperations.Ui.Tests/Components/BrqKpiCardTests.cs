using Bunit;
using PxOperations.Ui.Components.DataDisplay;

namespace PxOperations.Ui.Tests.Components;

/// <summary>
/// Trava a estrutura da faixa de indicadores no contrato do painel de NPS,
/// que é a referência visual da migração: <c>section.kpi-grid</c> com
/// <c>article.kpi</c> e sem ícone decorativo.
/// </summary>
public sealed class BrqKpiCardTests : BunitContext
{
    [Fact]
    public void Should_render_the_nps_dashboard_card_structure()
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
    }

    [Fact]
    public void Should_not_render_a_decorative_icon()
    {
        var cut = Render<BrqKpiBar>(parameters => parameters
            .AddChildContent<BrqKpiCard>(card => card
                .Add(component => component.Label, "Projetos")
                .Add(component => component.Value, "19")));

        Assert.Empty(cut.FindAll(".brq-kpi-icon"));
    }
}
