using Bunit;
using PxOperations.BlazorWasm.Features.Nps.Components;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

/// <summary>
/// F7/D6: distribuição na ordem detrator → neutro → promotor, com contagem,
/// percentual e a fórmula do NPS à vista.
/// </summary>
public sealed class NpsDistributionTests : TestContext
{
    public NpsDistributionTests()
    {
        // O app fixa pt-BR; o teste roda na mesma régua para verificar a
        // formatação que o usuário vê.
        var culture = new System.Globalization.CultureInfo("pt-BR");
        System.Globalization.CultureInfo.CurrentCulture = culture;
    }

    [Fact]
    public void Distribution_should_list_classes_in_the_ruler_order()
    {
        var cut = Render(detractors: 20, passives: 42, promoters: 111, nps: 52.6);

        var names = cut.FindAll(".dist__name").Select(n => n.TextContent.Trim()).ToArray();
        Assert.Equal(["Detrator (1 a 6)", "Neutro (7 a 8)", "Promotor (9 a 10)"], names);

        var counts = cut.FindAll(".dist__count").Select(n => n.TextContent.Trim()).ToArray();
        Assert.Equal(["20", "42", "111"], counts);
    }

    [Fact]
    public void Distribution_should_show_the_formula()
    {
        var cut = Render(detractors: 1, passives: 0, promoters: 1, nps: 0);

        var formula = cut.Find(".dist__formula").TextContent;
        Assert.Contains("50,0%", formula);
        Assert.Contains("NPS =", formula);
    }

    /// <summary>
    /// O app roda em pt-BR, onde o separador decimal é vírgula. "inline-size:
    /// 33,3%" é CSS inválido: a barra simplesmente some, sem erro no console e
    /// sem teste vermelho. A largura tem de sair em cultura invariante.
    /// </summary>
    [Fact]
    public void Segment_width_should_use_a_dot_so_the_css_is_valid()
    {
        var cut = Render(detractors: 1, passives: 1, promoters: 1, nps: 0);

        var style = cut.Find(".dist__seg--danger").GetAttribute("style");
        Assert.Contains("33.3%", style);
        Assert.DoesNotContain(",", style!);
    }

    [Fact]
    public void Empty_distribution_should_say_so_instead_of_drawing_an_empty_bar()
    {
        var cut = Render(0, 0, 0, 0);

        Assert.Empty(cut.FindAll(".dist__bar"));
        Assert.Contains("Sem respostas", cut.Find(".dist__empty").TextContent);
    }

    private IRenderedComponent<NpsDistribution> Render(int detractors, int passives, int promoters, double nps)
        => RenderComponent<NpsDistribution>(parameters => parameters
            .Add(c => c.Detractors, detractors)
            .Add(c => c.Passives, passives)
            .Add(c => c.Promoters, promoters)
            .Add(c => c.OfficialNps, nps));
}
