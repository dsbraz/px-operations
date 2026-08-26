using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

/// <summary>
/// Critério de aceite de F1, ao pé da letra: "marcar dois valores da mesma
/// faceta filtra pela união deles". Do lado do cliente isso quer dizer emitir
/// o parâmetro REPETIDO — juntar com vírgula chegaria ao servidor como um
/// valor só, e o filtro não casaria com nada.
/// </summary>
public sealed class NpsFacetFilterTests : TestContext
{
    public NpsFacetFilterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Marking_two_values_of_the_same_facet_should_send_both()
    {
        var cut = Render(out var handler);

        cut.Find("button.fmenu__btn").Click();
        CheckDc(cut, 0);
        CheckDc(cut, 1);

        cut.WaitForAssertion(() => Assert.Contains(handler.RequestUris, uri =>
            uri is not null
            && uri.AbsolutePath == "/api/nps/projects"
            && uri.Query.Contains("dc=DC1")
            && uri.Query.Contains("dc=DC2")));
    }

    [Fact]
    public void Two_values_of_the_same_facet_should_share_one_chip()
    {
        var cut = Render(out _);

        cut.Find("button.fmenu__btn").Click();
        CheckDc(cut, 0);
        CheckDc(cut, 1);

        // Um chip por faceta, juntando os valores: dois chips "DC" lado a lado
        // não diriam se a relação entre eles é união ou interseção.
        cut.WaitForAssertion(() =>
        {
            var chips = cut.FindAll(".filterbar__tokens .token");
            Assert.Single(chips);
            Assert.Contains("DC1, DC2", chips[0].TextContent);
        });
    }

    [Fact]
    public void Removing_a_facet_chip_should_drop_every_value_of_that_facet()
    {
        var cut = Render(out var handler);

        cut.Find("button.fmenu__btn").Click();
        CheckDc(cut, 0);
        CheckDc(cut, 1);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".filterbar__tokens .token")));

        cut.Find(".filterbar__tokens .token").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".filterbar__tokens .token"));
            var last = handler.RequestUris.Last(uri => uri?.AbsolutePath == "/api/nps/projects");
            Assert.DoesNotContain("dc=", last!.Query);
        });
    }

    [Fact]
    public void The_date_facet_should_not_be_offered_on_the_collection_tab()
    {
        // F1: a régua é a data da RESPOSTA. Na Coleta a faceta esvaziaria
        // justamente as colunas de quem ainda não respondeu.
        var cut = Render(out _, tab: "coleta");

        cut.Find("button.fmenu__btn").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Data de coleta", cut.Find(".fmenu__pop").TextContent));
    }

    [Fact]
    public void The_date_facet_should_send_a_resolved_range()
    {
        var cut = Render(out var handler);

        cut.Find("button.fmenu__btn").Click();
        cut.FindAll(".fmenu__pop input[type=radio]")[0].Click();

        cut.WaitForAssertion(() => Assert.Contains(handler.RequestUris, uri =>
            uri is not null
            && uri.AbsolutePath == "/api/nps/projects"
            && uri.Query.Contains("from=")
            && uri.Query.Contains("to=")));
    }

    /// <summary>
    /// Cada Change re-renderiza a página, e o bUnit invalida os handlers dos
    /// elementos achados antes disso: a busca precisa ser refeita a cada marca.
    /// </summary>
    private static void CheckDc(IRenderedComponent<NpsPage> cut, int index)
        => cut.Find(".fmenu__pop fieldset").QuerySelectorAll("input[type=checkbox]")[index].Change(true);

    private IRenderedComponent<NpsPage> Render(out ProjectsTestHelpers.MultiStubHttpMessageHandler handler, string tab = "resultados")
    {
        handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        for (var i = 0; i < 12; i++)
        {
            handler.AddResponse(HttpMethod.Get, DashboardJson, HttpStatusCode.OK);
            handler.AddResponse(HttpMethod.Get, ProjectsJson, HttpStatusCode.OK);
        }

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<NpsClient>();

        var cut = RenderComponent<NpsPage>(p => p.Add(x => x.Tab, tab));
        cut.WaitForAssertion(() => Assert.Contains("Projeto NPS", cut.Markup));
        return cut;
    }

    private const string DashboardJson = """
    {"totalProjects":1,"overdueProjects":1,"activeDispatches":0,"totalResponses":2,"officialNps":50.0,"averageScore":8.5,"detractors":0,"passives":1,"promoters":1}
    """;

    private const string ProjectsJson = """
    [{"id":1,"name":"Projeto NPS","client":"Cliente A","dc":"DC1","deliveryManager":"Maria","contactsCount":1,"activeDispatches":0,"linkTargetsCount":0,"answeredLinkTargetsCount":0,"responsesCount":0,"lastResponseAt":null,"lastNps":null,"isOverdue":true,"collectionStatus":"Pendente","isDismissed":false,"dismissalReason":null,"activeDispatchExpiresAt":null}]
    """;
}
