using Bunit;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

public sealed class NpsCollectionBoardTests : BunitContext
{
    [Fact]
    public void Board_should_keep_the_four_stage_columns_in_order()
    {
        var cut = Render<NpsCollectionBoard>();

        var titles = cut.FindAll(".nps-board-column .kanban-col-title")
            .Select(title => title.TextContent.Trim())
            .ToArray();
        Assert.Equal(new[] { "Sem link", "Aguardando resposta", "Recoleta", "Em dia" }, titles);
        Assert.Equal(4, cut.FindAll(".kanban-empty").Count);
    }

    [Fact]
    public void Board_should_place_a_project_in_its_stage_column()
    {
        var cut = Render<NpsCollectionBoard>(parameters => parameters
            .Add(board => board.Projects, [Project("awaiting_response")]));

        var awaiting = cut.FindAll(".nps-board-column")[1];
        Assert.Contains("Projeto Alfa", awaiting.TextContent);
        Assert.Equal("1", awaiting.QuerySelector(".kanban-col-count")!.TextContent.Trim());
        Assert.Equal(3, cut.FindAll(".kanban-empty").Count);
    }

    [Fact]
    public void Waived_projects_should_only_show_when_asked()
    {
        var cut = Render<NpsCollectionBoard>(parameters => parameters
            .Add(board => board.Projects, [Project("waived")]));

        Assert.Empty(cut.FindAll(".nps-waived-section"));

        cut.Render(parameters => parameters
            .Add(board => board.Projects, [Project("waived")])
            .Add(board => board.IncludeWaived, true));

        Assert.Contains("Projeto Alfa", cut.Find(".nps-waived-section").TextContent);
    }

    [Fact]
    public void Primary_action_should_report_the_project_it_belongs_to()
    {
        NpsProjectView? reported = null;
        var cut = Render<NpsCollectionBoard>(parameters => parameters
            .Add(board => board.Projects, [Project("awaiting_response")])
            .Add(board => board.OnPrimaryAction, project => reported = project));

        cut.Find(".nps-primary-action").Click();

        Assert.Equal("Projeto Alfa", reported?.Name);
    }

    private static NpsProjectView Project(string stage) => new()
    {
        Id = 1,
        Name = "Projeto Alfa",
        Client = "Cliente",
        Dc = "DC1",
        ProjectType = "Squad",
        ResponsesCount = 0,
        Stage = new NpsBadgeView { Code = stage, Label = "Etapa", Tone = "warning" },
        Temporal = new NpsTemporalView { Label = "Enviado há 2d", Tone = "warning" },
        ActiveLinks = [],
        PrimaryAction = new NpsPrimaryActionView { Code = "copy_link", Label = "Copiar link" },
        IsOverdue = false
    };
}
