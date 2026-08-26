using System.Globalization;
using Bunit;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps.Components;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

/// <summary>
/// F10/D12: tabela escaneável com o comentário só em prévia, e um detalhe que
/// abre com todos os campos respondidos.
/// </summary>
public sealed class NpsResponsesTests : TestContext
{
    public NpsResponsesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        // O app roda em pt-BR (Program.cs). Sem fixar a cultura aqui, a média
        // sairia "4.0" no teste e "4,0" na tela — e a asserção estaria
        // validando um formato que o usuário nunca vê.
        CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
    }

    [Fact]
    public void Each_row_should_name_the_project_it_came_from()
    {
        var cut = RenderComponent<NpsResponsesTable>(p => p
            .Add(x => x.Responses, [Response(projectName: "Projeto Alfa"), Response(projectName: "Projeto Beta")]));

        // Critério de aceite: "cada linha identifica claramente o projeto de
        // origem" — era o que faltava na lista solta que a tabela substitui.
        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Projeto Alfa", rows[0].TextContent);
        Assert.Contains("Projeto Beta", rows[1].TextContent);
    }

    [Fact]
    public void A_long_comment_should_be_truncated_in_the_table()
    {
        var longo = new string('x', 400);
        var cut = RenderComponent<NpsResponsesTable>(p => p
            .Add(x => x.Responses, [Response(comment: longo)]));

        var cell = cut.Find(".resp-comment").TextContent;
        Assert.True(cell.Length < longo.Length);
        Assert.EndsWith("…", cell);
    }

    [Fact]
    public void An_unidentified_response_should_read_as_anonymous()
    {
        var cut = RenderComponent<NpsResponsesTable>(p => p
            .Add(x => x.Responses, [Response()]));

        Assert.Contains("Resposta anônima", cut.Markup);
    }

    [Fact]
    public void Opening_a_row_should_hand_the_response_over()
    {
        NpsSurveyResponse? opened = null;
        var response = Response(projectName: "Projeto Alfa");
        var cut = RenderComponent<NpsResponsesTable>(p => p
            .Add(x => x.Responses, [response])
            .Add(x => x.OnOpen, r => opened = r));

        cut.Find("tbody tr").Click();

        Assert.Same(response, opened);
    }

    [Fact]
    public void The_detail_should_show_the_whole_comment_and_the_four_aspects()
    {
        var cut = RenderComponent<NpsResponseDetail>(p => p
            .Add(x => x.Response, Response(
                comment: "primeira linha\nsegunda linha",
                format: "Completo",
                quality: 5, schedule: 4, communication: 3, businessValue: 4)));

        Assert.Contains("primeira linha", cut.Markup);
        Assert.Contains("segunda linha", cut.Markup);
        Assert.Equal(4, cut.FindAll(".resp-detail__aspects dl > div").Count);
        // Média da ENTREGA, distinta da nota de recomendação: (5+4+3+4)/4 = 4,0
        Assert.Contains("Média 4,0 de 5", cut.Find(".resp-detail__aspects-head span").TextContent);
    }

    [Fact]
    public void A_simplified_response_should_not_show_the_aspects_block()
    {
        var cut = RenderComponent<NpsResponseDetail>(p => p
            .Add(x => x.Response, Response(format: "Simplificado")));

        // Critério de aceite explícito: o formato Simplificado não pergunta
        // aspecto nenhum, então o bloco não existe — não é "tudo vazio".
        Assert.Empty(cut.FindAll(".resp-detail__aspects"));
    }

    [Fact]
    public void An_aspect_left_blank_should_not_be_listed()
    {
        var cut = RenderComponent<NpsResponseDetail>(p => p
            .Add(x => x.Response, Response(format: "Completo", quality: 5, schedule: 3)));

        // Os aspectos são opcionais mesmo no Completo. Mostrar "—" para um que
        // foi pulado sugeriria nota baixa, e a média mentiria junto.
        Assert.Equal(2, cut.FindAll(".resp-detail__aspects dl > div").Count);
        Assert.Contains("Média 4,0 de 5", cut.Find(".resp-detail__aspects-head span").TextContent);
    }

    [Fact]
    public void Escape_should_close_the_detail()
    {
        var closed = false;
        var cut = RenderComponent<NpsResponseDetail>(p => p
            .Add(x => x.Response, Response())
            .Add(x => x.OnClose, () => closed = true));

        cut.Find(".modal").KeyDown(key: "Escape");

        Assert.True(closed);
    }

    private static NpsSurveyResponse Response(
        string projectName = "Projeto",
        string? comment = null,
        string format = "Simplificado",
        string classification = "Promotor",
        int score = 9,
        string? respondentName = null,
        int? quality = null,
        int? schedule = null,
        int? communication = null,
        int? businessValue = null)
        => new()
        {
            Id = 1,
            ProjectId = 1,
            ProjectName = projectName,
            DispatchId = 1,
            TargetId = 1,
            Score = score,
            Classification = classification,
            Format = format,
            Comment = comment,
            RespondentName = respondentName,
            Quality = quality,
            Schedule = schedule,
            Communication = communication,
            BusinessValue = businessValue,
            SubmittedAt = "2026-08-26T10:00:00Z"
        };
}
