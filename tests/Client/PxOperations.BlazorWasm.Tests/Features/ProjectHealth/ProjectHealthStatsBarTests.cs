using Bunit;
using PxOperations.BlazorWasm.Features.ProjectHealth;

namespace PxOperations.BlazorWasm.Tests.Features.ProjectHealth;

/// <summary>
/// Críticos e Sem resposta sinalizam o que pede atenção; os outros
/// indicadores são contagem neutra.
/// </summary>
public sealed class ProjectHealthStatsBarTests : BunitContext
{
    [Fact]
    public void Should_tone_only_the_critical_and_no_response_indicators()
    {
        var cut = Render<ProjectHealthStatsBar>(parameters => parameters
            .Add(bar => bar.TotalProjects, 10)
            .Add(bar => bar.AverageScore, 7.5)
            .Add(bar => bar.CriticalCount, 2)
            .Add(bar => bar.NoResponseCount, 3));

        var tones = cut.FindAll("article.kpi")
            .ToDictionary(card => card.QuerySelector(".kpi__label")!.TextContent, card => card.ClassName);

        Assert.Equal("kpi", tones["Projetos"]);
        Assert.Equal("kpi", tones["Média / 10"]);
        Assert.Equal("kpi kpi--danger", tones["Críticos"]);
        Assert.Equal("kpi kpi--warn", tones["Sem resp"]);
    }
}
