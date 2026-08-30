using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

public sealed class NpsResultsViewTests : BunitContext
{
    public NpsResultsViewTests()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, "[]", HttpStatusCode.OK);
        Services.AddScoped(_ => new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
        Services.AddScoped<NpsClient>();
    }

    [Fact]
    public void Sorting_a_column_should_flip_only_that_column_aria_sort()
    {
        var cut = Render<NpsResultsView>(parameters => parameters
            .Add(view => view.Dashboard, Dashboard())
            .Add(view => view.Results, [Result(1, "Zulu"), Result(2, "Alpha")]));

        Assert.Equal("ascending", cut.Find("th[data-column=project]").GetAttribute("aria-sort"));
        Assert.Equal("none", cut.Find("th[data-column=nps]").GetAttribute("aria-sort"));

        cut.Find("button[data-sort=project]").Click();

        Assert.Equal("descending", cut.Find("th[data-column=project]").GetAttribute("aria-sort"));
    }

    [Fact]
    public void Rows_should_start_in_the_name_order_the_server_sent()
    {
        var cut = Render<NpsResultsView>(parameters => parameters
            .Add(view => view.Dashboard, Dashboard())
            .Add(view => view.Results, [Result(1, "Zulu"), Result(2, "Alpha")]));

        var names = cut.FindAll(".nps-result-row .nps-result-project")
            .Select(cell => cell.TextContent.Trim())
            .ToArray();
        Assert.Equal(new[] { "Alpha", "Zulu" }, names);
    }

    /// <summary>
    /// A largura do segmento vai para um atributo style: em cultura pt-BR uma
    /// vírgula tornaria a declaração inválida e a barra sumiria.
    /// </summary>
    [Fact]
    public void Distribution_width_should_use_an_invariant_decimal_separator()
    {
        var cut = Render<NpsResultsView>(parameters => parameters
            .Add(view => view.Dashboard, Dashboard(33.333))
            .Add(view => view.Results, []));

        var style = cut.Find(".nps-distribution-bar span").GetAttribute("style");
        Assert.Equal("width:33.333%", style);
    }

    private static NpsDashboardView Dashboard(double percentage = 100) => new()
    {
        OfficialNps = 100,
        TotalResponses = 1,
        AverageScore = 9,
        OverdueProjects = 0,
        Scale = new NpsScaleView { Minimum = 1, Maximum = 10 },
        Distribution =
        [
            new NpsDistributionView { Code = "promoter", Label = "Promotor", Tone = "positive", Count = 1, Percentage = percentage }
        ],
        AspectSummary = new NpsAspectSummaryView
        {
            CompleteResponsesCount = 0,
            Scale = new NpsScaleView { Minimum = 1, Maximum = 5 },
            Aspects = []
        },
        FilterOptions = new NpsFilterOptionsView()
    };

    private static NpsProjectResultView Result(int id, string name) => new()
    {
        Id = id,
        Name = name,
        Client = "Cliente",
        Dc = "DC1",
        ResponsesCount = 1,
        OfficialNps = 100,
        Distribution = [],
        Formats = [],
        Status = new NpsBadgeView { Code = "responded", Label = "Respondido", Tone = "positive" }
    };
}
