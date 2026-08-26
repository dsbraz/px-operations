using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

/// <summary>
/// F1/D5: três subpáginas com URL própria. Critério de aceite: abrir
/// /nps/resultados direto carrega a subpágina certa, e os filtros persistem ao
/// trocar de subpágina.
/// </summary>
public sealed class NpsRoutingTests : TestContext
{
    public NpsRoutingTests()
    {
        // A barra de filtros usa BrqFilterPanel, que importa um módulo JS para
        // fechar ao clicar fora.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Theory]
    [InlineData(null, "coleta")]
    [InlineData("coleta", "coleta")]
    [InlineData("resultados", "resultados")]
    [InlineData("respostas", "respostas")]
    public void Route_should_select_the_matching_subpage(string? tab, string expected)
    {
        var cut = RenderPage(tab);

        cut.WaitForAssertion(() =>
        {
            var selected = cut.Find("a.tab[aria-selected='true']");
            Assert.Equal($"/nps/{expected}", selected.GetAttribute("href"));
        });
    }

    /// <summary>
    /// Rota desconhecida cai na primeira subpágina em vez de renderizar vazio —
    /// o protótipo abre em Coleta, que é a tela de trabalho do operador.
    /// </summary>
    [Fact]
    public void Unknown_subpage_should_fall_back_to_collection()
    {
        var cut = RenderPage("inexistente");

        cut.WaitForAssertion(() =>
            Assert.Equal("/nps/coleta", cut.Find("a.tab[aria-selected='true']").GetAttribute("href")));
    }

    /// <summary>
    /// D11: as abas são âncoras com href, não botões. É o que dá voltar/avançar
    /// do navegador e link compartilhável para uma subpágina.
    /// </summary>
    [Fact]
    public void Tabs_should_be_links_so_history_works()
    {
        var cut = RenderPage(null);

        cut.WaitForAssertion(() =>
        {
            var tabs = cut.FindAll("a.tab");
            Assert.Equal(3, tabs.Count);
            Assert.All(tabs, tab => Assert.StartsWith("/nps/", tab.GetAttribute("href")));
        });
    }

    private IRenderedComponent<NpsPage> RenderPage(string? tab)
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "[]", HttpStatusCode.OK);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<NpsClient>();

        return RenderComponent<NpsPage>(parameters => parameters.Add(page => page.Tab, tab));
    }

    private static string DashboardJson() => """
    {"totalProjects":0,"overdueProjects":0,"activeDispatches":0,"totalResponses":0,"officialNps":0.0,"averageScore":0.0,"detractors":0,"passives":0,"promoters":0}
    """;
}
