using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

public sealed class NpsPageTests : TestContext
{
    public NpsPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Root_route_should_redirect_to_collection_without_loading_data()
    {
        var handler = RegisterClient();
        Navigate("/nps?client=Alpha");

        RenderComponent<NpsPage>();

        Assert.EndsWith("/nps/coleta?client=Alpha", Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Empty(handler.RequestUris);
    }

    [Fact]
    public void Collection_should_keep_four_columns_and_render_waived_projects_below_them()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", ProjectsJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta?includeWaived=true");

        var cut = RenderComponent<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("h1"));
            Assert.Equal("NPS", cut.Find("h1").TextContent.Trim());
            Assert.Equal(4, cut.FindAll(".nps-board-column").Count);
            Assert.Contains("Sem link", cut.FindAll(".nps-board-column")[0].TextContent);
            Assert.Contains("Aguardando resposta", cut.FindAll(".nps-board-column")[1].TextContent);
            Assert.Contains("Recoleta", cut.FindAll(".nps-board-column")[2].TextContent);
            Assert.Contains("Em dia", cut.FindAll(".nps-board-column")[3].TextContent);
            Assert.Contains("Projeto ativo", cut.Find(".nps-board").TextContent);
            Assert.DoesNotContain("Projeto dispensado", cut.Find(".nps-board").TextContent);
            Assert.Contains("Projeto dispensado", cut.Find(".nps-waived-section").TextContent);
        });
    }

    [Fact]
    public void Shared_filters_should_write_repeated_parameters_show_one_chip_per_facet_and_clear_everything()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", ProjectsJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta?client=Alpha&client=Beta&format=complete");

        var cut = RenderComponent<NpsPage>();
        cut.WaitForAssertion(() => Assert.Contains("Alpha, Beta", cut.Find(".nps-filter-chip").TextContent));

        Assert.Single(cut.FindAll(".nps-filter-chip"));
        cut.Find(".fmenu__btn").Click();
        cut.Find(".fmenu__clear-sel").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.EndsWith("/nps/coleta", Services.GetRequiredService<NavigationManager>().Uri);
            Assert.Empty(cut.FindAll(".nps-filter-chip"));
        });
    }

    [Fact]
    public void Results_should_render_only_four_kpis_formula_and_ordered_distribution()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", DashboardJson(), HttpStatusCode.OK);
        Navigate("/nps/resultados");

        var cut = RenderComponent<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(4, cut.FindAll(".nps-kpi").Count);
            Assert.Contains("% promotores − % detratores", cut.Markup);
            var items = cut.FindAll(".nps-distribution-legend li");
            Assert.Equal(new[] { "Detrator", "Neutro", "Promotor" }, items.Select(item => item.QuerySelector("strong")!.TextContent));
            Assert.DoesNotContain("Média por aspecto", cut.Markup);
            Assert.DoesNotContain("Drill-down", cut.Markup);
            Assert.Empty(cut.FindAll("table"));
        });
    }

    [Fact]
    public void Responses_should_render_the_exact_placeholder_without_calling_a_global_query()
    {
        var handler = RegisterClient();
        Navigate("/nps/respostas?format=complete");

        var cut = RenderComponent<NpsPage>();

        Assert.Contains("A tabela de auditoria chega numa entrega seguinte.", cut.Markup);
        Assert.Empty(cut.FindAll(".nps-filter-toolbar"));
        Assert.Empty(cut.FindAll("table"));
        Assert.Empty(handler.RequestUris);
    }

    [Fact]
    public void Generate_link_should_have_two_steps_and_use_server_expiration_for_the_message()
    {
        var token = Guid.NewGuid();
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", ProjectsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Post, "/api/nps/dispatches", DispatchJson(token), HttpStatusCode.Created);
        Navigate("/nps/coleta");

        var cut = RenderComponent<NpsPage>();
        cut.WaitForAssertion(() => Assert.Contains("Projeto ativo", cut.Markup));
        cut.Find(".nps-page-actions button").Click();

        cut.WaitForAssertion(() => Assert.Contains("Projeto, formato e idioma", cut.Markup));
        cut.Find(".nps-create-project").Change("1");
        cut.Find(".nps-create-submit").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(token.ToString(), cut.Markup);
            Assert.Contains("21/08/2026", cut.Markup);
            Assert.Contains("Copiar link", cut.Markup);
            Assert.Contains("Copiar mensagem", cut.Markup);
            Assert.Contains("Completo", cut.Markup);
        });
    }

    [Fact]
    public void Api_failure_should_keep_page_structure_and_offer_retry_without_demo_data()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", "{}", HttpStatusCode.InternalServerError);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", "{}", HttpStatusCode.InternalServerError);
        Navigate("/nps/coleta");

        var cut = RenderComponent<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(4, cut.FindAll(".nps-board-column").Count);
            Assert.Contains("Tentar novamente", cut.Markup);
            Assert.DoesNotContain("Projeto ativo", cut.Markup);
        });
    }

    private ProjectsTestHelpers.MultiStubHttpMessageHandler RegisterClient()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        Services.AddScoped(_ => httpClient);
        Services.AddScoped<NpsClient>();
        return handler;
    }

    private void Navigate(string path)
        => Services.GetRequiredService<NavigationManager>().NavigateTo($"http://localhost{path}");

    private static string DashboardJson() => """
        {
          "officialNps":50.0,"totalResponses":4,"averageScore":8.3,"overdueProjects":1,
          "scale":{"minimum":1,"maximum":10},
          "distribution":[
            {"code":"detractor","label":"Detrator","tone":"critical","count":1,"percentage":25.0},
            {"code":"passive","label":"Neutro","tone":"warning","count":1,"percentage":25.0},
            {"code":"promoter","label":"Promotor","tone":"positive","count":2,"percentage":50.0}
          ],
          "filterOptions":{
            "clients":[{"code":"Alpha","label":"Alpha"},{"code":"Beta","label":"Beta"}],
            "dcs":[{"code":"DC1","label":"DC1"}],"projectTypes":[],"deliveryManagers":[],
            "statuses":[],"formats":[],"classifications":[]
          }
        }
        """;

    private static string ProjectsJson() => """
        [
          {
            "id":1,"name":"Projeto ativo","client":"Alpha","dc":"DC1","deliveryManager":"Maria","projectType":"Squad","responsesCount":0,
            "stage":{"code":"no_link","label":"Sem link","tone":"neutral"},
            "temporal":{"label":"Nunca coletado","tone":"neutral","at":null},"waiver":null,"activeLinks":[],
            "primaryAction":{"code":"generate_link","label":"Gerar link","format":"complete","dispatchId":null,"token":null},
            "isOverdue":true,"lastDispatchClosedAt":null
          },
          {
            "id":2,"name":"Projeto dispensado","client":"Beta","dc":"DC1","deliveryManager":"João","projectType":"Squad","responsesCount":1,
            "stage":{"code":"waived","label":"Dispensado","tone":"neutral"},
            "temporal":{"label":"Dispensado em 01/08/2026","tone":"neutral","at":"2026-08-01T12:00:00Z"},
            "waiver":{"reason":"Sem pesquisa","waivedAt":"2026-08-01T12:00:00Z"},"activeLinks":[],
            "primaryAction":{"code":"reactivate","label":"Reativar","format":null,"dispatchId":null,"token":null},
            "isOverdue":false,"lastDispatchClosedAt":"2026-08-01T12:00:00Z"
          }
        ]
        """;

    private static string DispatchJson(Guid token) => $$"""
        {
          "dispatch":{"id":10,"projectId":1,"projectName":"Projeto ativo","format":"complete","formatLabel":"Completo","language":"pt","languageLabel":"Português","status":"open","createdAt":"2026-08-01T12:00:00Z","expiresAt":"2026-08-21T12:00:00Z","closedAt":null,"targetsCount":1,"responsesCount":0,"availability":"open","availabilityLabel":"Aberto","tone":"positive"},
          "targets":[{"id":20,"dispatchId":10,"contactId":null,"contactName":null,"contactEmail":null,"token":"{{token}}","isGeneric":true,"responsesCount":0}]
        }
        """;
}
