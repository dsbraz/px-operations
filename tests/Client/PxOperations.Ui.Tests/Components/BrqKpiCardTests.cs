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

    /// <summary>
    /// O tom é estado, não decoração: só o card que sinaliza algo ganha cor,
    /// e o rótulo continua dizendo o que é (nunca só cor).
    /// </summary>
    [Fact]
    public void Should_mark_only_the_cards_that_carry_a_tone()
    {
        var cut = Render<BrqKpiBar>(parameters => parameters
            .AddChildContent<BrqKpiCard>(card => card
                .Add(component => component.Label, "Projetos")
                .Add(component => component.Value, "19"))
            .AddChildContent<BrqKpiCard>(card => card
                .Add(component => component.Label, "Críticos")
                .Add(component => component.Value, "2")
                .Add(component => component.Tone, BrqStatusTone.Danger))
            .AddChildContent<BrqKpiCard>(card => card
                .Add(component => component.Label, "Sem resp")
                .Add(component => component.Value, "3")
                .Add(component => component.Tone, BrqStatusTone.Warning)));

        var cards = cut.FindAll("article.kpi");
        Assert.Equal("kpi", cards[0].ClassName);
        Assert.Equal("kpi kpi--danger", cards[1].ClassName);
        Assert.Equal("kpi kpi--warn", cards[2].ClassName);
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
