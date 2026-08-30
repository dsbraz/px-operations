using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

public sealed class NpsResponsesViewTests : BunitContext
{
    [Fact]
    public void Empty_result_should_offer_the_empty_state_instead_of_a_table()
    {
        var cut = Render<NpsResponsesView>();

        Assert.NotNull(cut.Find(".nps-empty-delivery"));
        Assert.Empty(cut.FindAll(".nps-responses-table"));
    }

    [Fact]
    public void Row_should_take_the_tone_the_server_sent()
    {
        var cut = Render<NpsResponsesView>(parameters => parameters
            .Add(view => view.Responses, [Response(tone: "warning")]));

        Assert.Contains("pill--warn", cut.Find(".nps-response-row .pill").ClassList);
    }

    [Fact]
    public void Anonymous_response_should_say_so_in_the_author_column()
    {
        var cut = Render<NpsResponsesView>(parameters => parameters
            .Add(view => view.Responses, [Response(name: null, email: null)]));

        Assert.Equal("Resposta anônima", cut.Find(".nps-response-author span").TextContent.Trim());
    }

    [Fact]
    public void Pressing_enter_on_a_row_should_open_the_response()
    {
        NpsResponseView? opened = null;
        var cut = Render<NpsResponsesView>(parameters => parameters
            .Add(view => view.Responses, [Response()])
            .Add(view => view.OnOpenResponse, response => opened = response));

        cut.Find(".nps-response-row").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal("Projeto Alfa", opened?.ProjectName);
    }

    private static NpsResponseView Response(
        string? name = "Pessoa",
        string? email = "pessoa@example.com",
        string tone = "positive") => new()
    {
        Id = 1,
        ProjectId = 1,
        ProjectName = "Projeto Alfa",
        DispatchId = 1,
        TargetId = 1,
        Format = "complete",
        FormatLabel = "Completo",
        Score = 9,
        Classification = "promoter",
        ClassificationLabel = "Promotor",
        ClassificationTone = tone,
        RespondentName = name,
        RespondentEmail = email,
        SubmittedAt = new DateTimeOffset(2026, 8, 21, 12, 0, 0, TimeSpan.Zero)
    };
}
