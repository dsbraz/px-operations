using Bunit;
using PxOperations.BlazorWasm.Features.Nps;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

public sealed class NpsStatsBarTests : BunitContext
{
    [Fact]
    public void Bar_should_render_the_four_indicators_in_order()
    {
        var cut = Render<NpsStatsBar>(parameters => parameters
            .Add(bar => bar.OfficialNps, 50.0)
            .Add(bar => bar.TotalResponses, 4)
            .Add(bar => bar.AverageScore, 8.25)
            .Add(bar => bar.OverdueProjects, 1));

        var labels = cut.FindAll(".stat .slbl").Select(cell => cell.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "NPS oficial", "Respostas", "Média", "Vencidos" }, labels);

        var values = cut.FindAll(".stat .sval").Select(cell => cell.TextContent.Trim()).ToArray();
        Assert.Equal("4", values[1]);
        Assert.Equal("1", values[3]);
    }

    [Fact]
    public void Bar_should_show_a_dash_while_the_dashboard_has_not_loaded()
    {
        var cut = Render<NpsStatsBar>();

        Assert.All(
            cut.FindAll(".stat .sval").Select(cell => cell.TextContent.Trim()),
            value => Assert.Equal("—", value));
    }
}
