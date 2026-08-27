using System.Globalization;
using Bunit;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps.Components;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

/// <summary>
/// F9: médias por aspecto com o recorte explícito no subtítulo. O painel só
/// liga quando há aspecto respondido.
/// </summary>
public sealed class NpsAspectAveragesTests : TestContext
{
    public NpsAspectAveragesTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
    }

    [Fact]
    public void The_panel_should_show_the_four_aspects_with_the_recorte_in_the_subtitle()
    {
        var cut = Render(Dashboard(completeResponses: 12, quality: 4.6, schedule: 3.3, communication: 3.8, businessValue: 4.3));

        Assert.Equal(4, cut.FindAll(".aspects__list li").Count);
        Assert.Contains("Formulário completo · 12 respostas · escala de 1 a 5", cut.Find(".aspects__sub").TextContent);
        Assert.Contains("4,6", cut.Markup);
    }

    [Fact]
    public void The_panel_should_stay_hidden_when_no_aspect_was_answered()
    {
        // "O painel só liga quando os dois existirem" — sem aspecto respondido
        // ele mostraria quatro barras vazias com cara de nota baixa.
        var cut = Render(Dashboard(completeResponses: 0));

        Assert.Empty(cut.FindAll(".aspects"));
    }

    [Fact]
    public void An_unanswered_aspect_should_read_as_absent_not_as_zero()
    {
        var cut = Render(Dashboard(completeResponses: 3, quality: 4.0, qualityCount: 3));

        var prazos = cut.FindAll(".aspects__list li")[1];
        Assert.Contains("sem resposta", prazos.TextContent);
        Assert.Empty(prazos.QuerySelectorAll(".aspects__bar"));
    }

    [Fact]
    public void An_aspect_with_fewer_answers_should_say_how_many()
    {
        // O aspecto é opcional mesmo no Completo: uma média sobre menos gente
        // não pode passar por média de todos.
        var cut = Render(Dashboard(completeResponses: 10, quality: 4.0, qualityCount: 10, schedule: 2.0, scheduleCount: 4));

        Assert.DoesNotContain("respostas", cut.FindAll(".aspects__list li")[0].QuerySelector(".aspects__value")!.TextContent);
        Assert.Contains("4 respostas", cut.FindAll(".aspects__list li")[1].TextContent);
    }

    [Fact]
    public void The_bar_width_should_be_valid_css_under_pt_br()
    {
        var cut = Render(Dashboard(completeResponses: 1, quality: 4.4, qualityCount: 1));

        // Sob pt-BR o decimal sai com vírgula, e "inline-size: 88,0%" não é CSS
        // válido: a barra sumiria sem erro nenhum.
        var estilo = cut.Find(".aspects__bar").GetAttribute("style")!;
        Assert.Contains("88%", estilo);
        Assert.DoesNotContain(",", estilo);
    }

    private IRenderedComponent<NpsAspectAverages> Render(NpsDashboardResponse dashboard)
        => RenderComponent<NpsAspectAverages>(p => p.Add(x => x.Dashboard, dashboard));

    private static NpsDashboardResponse Dashboard(
        int completeResponses,
        double? quality = null, int qualityCount = 0,
        double? schedule = null, int scheduleCount = 0,
        double? communication = null, int communicationCount = 0,
        double? businessValue = null, int businessValueCount = 0)
        => new()
        {
            TotalProjects = 1,
            TotalResponses = completeResponses,
            CompleteResponses = completeResponses,
            QualityAverage = quality,
            QualityCount = quality is null ? qualityCount : Math.Max(qualityCount, 1),
            ScheduleAverage = schedule,
            ScheduleCount = schedule is null ? scheduleCount : Math.Max(scheduleCount, 1),
            CommunicationAverage = communication,
            CommunicationCount = communication is null ? communicationCount : Math.Max(communicationCount, 1),
            BusinessValueAverage = businessValue,
            BusinessValueCount = businessValue is null ? businessValueCount : Math.Max(businessValueCount, 1)
        };
}
