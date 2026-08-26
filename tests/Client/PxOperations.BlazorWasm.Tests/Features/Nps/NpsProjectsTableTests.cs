using Bunit;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps.Components;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

/// <summary>
/// F8: tabela ordenável com drill-down "todas as notas". Uma expansão por vez,
/// e só para projeto com respostas.
/// </summary>
public sealed class NpsProjectsTableTests : TestContext
{
    [Fact]
    public void Expanding_a_project_should_show_its_individual_notes()
    {
        var cut = Render([Project(1, "Alfa", responses: 2, nps: 50)], _ =>
            [Response(9, "Promotor", "muito bom"), Response(4, "Detrator", "atrasou")]);

        cut.Find("tbody .btn-link").Click();

        cut.WaitForAssertion(() =>
        {
            var notas = cut.FindAll(".nps-drill__notes li");
            Assert.Equal(2, notas.Count);
            Assert.Contains("muito bom", cut.Markup);
            Assert.Contains("atrasou", cut.Markup);
        });
    }

    [Fact]
    public void Only_one_project_should_stay_expanded()
    {
        var cut = Render(
            [Project(1, "Alfa", responses: 1, nps: 100), Project(2, "Beta", responses: 1, nps: 0)],
            _ => [Response(10, "Promotor", "nota")]);

        cut.FindAll("tbody .btn-link")[0].Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".nps-drill")));

        cut.FindAll("tbody .btn-link")[1].Click();

        // Duas abertas transformariam a tabela numa lista de listas.
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".nps-drill")));
    }

    [Fact]
    public void Clicking_the_expanded_project_again_should_collapse_it()
    {
        var cut = Render([Project(1, "Alfa", responses: 1, nps: 100)], _ => [Response(10, "Promotor", "nota")]);

        cut.Find("tbody .btn-link").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".nps-drill")));

        cut.Find("tbody .btn-link").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".nps-drill")));
    }

    [Fact]
    public void A_project_without_responses_should_not_be_expandable()
    {
        // F8 diz "clicar num projeto COM respostas": sem nenhuma, expandir
        // abriria um painel vazio.
        var cut = Render([Project(1, "Alfa", responses: 0, nps: null)], _ => []);

        Assert.Empty(cut.FindAll("tbody .btn-link").Where(b => b.TextContent.Contains("Alfa")));
    }

    [Fact]
    public void Changing_the_filter_should_drop_a_stale_expansion()
    {
        var cut = Render([Project(1, "Alfa", responses: 1, nps: 100)], _ => [Response(10, "Promotor", "nota")]);

        cut.Find("tbody .btn-link").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".nps-drill")));

        // O projeto expandido sai do recorte. Manter as notas abertas ao lado
        // de um NPS recalculado quebraria o critério de aceite sem avisar.
        cut.SetParametersAndRender(p => p
            .Add(x => x.Projects, [Project(2, "Beta", responses: 1, nps: 0)])
            .Add(x => x.LoadResponses, _ => Task.FromResult<IReadOnlyList<NpsSurveyResponse>>([])));

        Assert.Empty(cut.FindAll(".nps-drill"));
    }

    [Fact]
    public void Sorting_by_nps_should_reorder_and_announce_itself()
    {
        var cut = Render(
            [Project(1, "Alfa", responses: 1, nps: 10), Project(2, "Beta", responses: 1, nps: 90)],
            _ => []);

        // Abre pelo NPS decrescente: o pior e o melhor resultado são o que se
        // procura primeiro, e o maior fica no topo.
        Assert.Equal("descending", NpsHeader(cut).GetAttribute("aria-sort"));
        Assert.Contains("Beta", cut.FindAll("tbody tr")[0].TextContent);

        NpsHeader(cut).QuerySelector("button")!.Click();

        // Clicar na coluna já ordenada inverte. aria-sort é o que um leitor de
        // tela usa para saber por onde a tabela está; a seta não diz nada a ele.
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("ascending", NpsHeader(cut).GetAttribute("aria-sort"));
            Assert.Contains("Alfa", cut.FindAll("tbody tr")[0].TextContent);
        });
    }

    private static AngleSharp.Dom.IElement NpsHeader(IRenderedComponent<NpsProjectsTable> cut)
        => cut.FindAll("th").Single(h => h.TextContent.Trim().StartsWith("NPS"));

    private IRenderedComponent<NpsProjectsTable> Render(
        IReadOnlyList<NpsProjectResponse> projects,
        Func<int, IReadOnlyList<NpsSurveyResponse>> load)
        => RenderComponent<NpsProjectsTable>(p => p
            .Add(x => x.Projects, projects)
            .Add(x => x.LoadResponses, id => Task.FromResult(load(id))));

    private static NpsProjectResponse Project(int id, string name, int responses, double? nps)
        => new()
        {
            Id = id,
            Name = name,
            Client = "Cliente",
            Dc = "DC1",
            DeliveryManager = "Maria",
            ResponsesCount = responses,
            LastNps = nps,
            CollectionStatus = responses > 0 ? "Respondido" : "Pendente"
        };

    private static NpsSurveyResponse Response(int score, string classification, string comment)
        => new()
        {
            Id = score,
            ProjectId = 1,
            ProjectName = "Alfa",
            DispatchId = 1,
            TargetId = 1,
            Score = score,
            Classification = classification,
            Format = "Simplificado",
            Comment = comment,
            SubmittedAt = "2026-08-26T10:00:00Z"
        };
}
