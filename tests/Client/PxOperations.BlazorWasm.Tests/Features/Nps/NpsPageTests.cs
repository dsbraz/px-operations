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
            Assert.Equal(4, cut.FindAll(".nps-indicators .stat").Count);
            Assert.Equal(4, cut.FindAll(".nps-board-column").Count);
            Assert.Contains("kb-gray", cut.FindAll(".nps-board-column")[0].ClassList);
            Assert.Contains("kb-orange", cut.FindAll(".nps-board-column")[1].ClassList);
            Assert.Contains("kb-purple", cut.FindAll(".nps-board-column")[2].ClassList);
            Assert.Contains("kb-green", cut.FindAll(".nps-board-column")[3].ClassList);
            Assert.Contains("Sem link", cut.FindAll(".nps-board-column")[0].TextContent);
            Assert.Contains("Aguardando resposta", cut.FindAll(".nps-board-column")[1].TextContent);
            Assert.Contains("Recoleta", cut.FindAll(".nps-board-column")[2].TextContent);
            Assert.Contains("Em dia", cut.FindAll(".nps-board-column")[3].TextContent);
            Assert.Equal(3, cut.FindAll(".nps-board-column .kanban-empty").Count);
            Assert.Contains("Projeto ativo", cut.Find(".nps-board").TextContent);
            Assert.DoesNotContain("Projeto dispensado", cut.Find(".nps-board").TextContent);
            Assert.Contains("Projeto dispensado", cut.Find(".nps-waived-section").TextContent);
        });
    }

    [Fact]
    public void Collection_should_order_actions_indicators_toolbar_and_content_with_tabs_inside_toolbar()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", ProjectsJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta");

        var cut = RenderComponent<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            var markup = cut.Markup;
            var actions = markup.IndexOf("nps-page-header", StringComparison.Ordinal);
            var indicators = markup.IndexOf("nps-indicators", StringComparison.Ordinal);
            var toolbar = markup.IndexOf("nps-toolbar", StringComparison.Ordinal);
            var content = markup.IndexOf("nps-content", StringComparison.Ordinal);

            Assert.True(actions < indicators, "A barra de ações deve vir antes dos indicadores.");
            Assert.True(indicators < toolbar, "Os indicadores devem vir antes da toolbar.");
            Assert.True(toolbar < content, "A toolbar deve vir antes do conteúdo.");
            Assert.Contains("nps-toolbar", cut.Find(".view-tabs").ParentElement!.ClassList);
        });
    }

    [Fact]
    public void Collection_search_should_match_the_other_toolbars_without_an_extra_visible_label()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", ProjectsJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta");

        var cut = RenderComponent<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            var search = cut.Find(".nps-search");
            Assert.Equal("DIV", search.TagName);
            Assert.Empty(search.QuerySelectorAll(":scope > span"));
            Assert.Equal("Buscar projeto ou cliente", search.QuerySelector("input")!.GetAttribute("placeholder"));
        });
    }

    [Fact]
    public void Collection_card_should_highlight_the_primary_action_and_expose_two_accessible_icon_actions()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", ProjectsJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta");

        var cut = RenderComponent<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            var card = cut.Find(".nps-project-card");
            Assert.Equal("Gerar link", card.QuerySelector(".nps-primary-action")!.TextContent.Trim());
            var iconActions = card.QuerySelectorAll(".nps-card-icon-action");
            Assert.Equal(2, iconActions.Length);
            Assert.All(iconActions, action =>
            {
                Assert.False(string.IsNullOrWhiteSpace(action.GetAttribute("title")));
                Assert.False(string.IsNullOrWhiteSpace(action.GetAttribute("aria-label")));
                Assert.NotNull(action.QuerySelector("svg"));
            });
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
    public void Results_should_keep_indicators_and_executive_panel_then_render_the_project_table()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/project-results", ProjectResultsJson(), HttpStatusCode.OK);
        Navigate("/nps/resultados");

        var cut = RenderComponent<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(4, cut.FindAll(".nps-indicators .stat").Count);
            Assert.Empty(cut.FindAll(".nps-results .nps-kpi"));
            Assert.Equal("Distribuição das respostas", cut.Find(".nps-results h2").TextContent.Trim());
            Assert.Contains("% promotores − % detratores", cut.Markup);
            var items = cut.FindAll(".nps-distribution-legend li");
            Assert.Equal(new[] { "Detrator", "Neutro", "Promotor" }, items.Select(item => item.QuerySelector("strong")!.TextContent));
            Assert.Equal(3, cut.FindAll(".nps-legend-swatch").Count);
            Assert.DoesNotContain("Média por aspecto", cut.Markup);
            var headers = cut.FindAll(".nps-results-table thead th");
            Assert.Equal(new[] { "Projeto", "Cliente", "DC", "DM", "Respostas", "NPS", "Status" }, headers.Select(header => header.TextContent.Trim()));
            Assert.Equal("ascending", headers[0].GetAttribute("aria-sort"));
            Assert.Equal(new[] { "Alpha", "Zulu" }, cut.FindAll(".nps-result-row .nps-result-project").Select(cell => cell.TextContent.Trim()));
        });
    }

    [Fact]
    public void Results_should_sort_accessibly_and_keep_only_one_lazy_expansion_open()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/project-results", ProjectResultsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects/1/responses", FilteredResponsesJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects/2/responses", FilteredResponsesJson(), HttpStatusCode.OK);
        Navigate("/nps/resultados?from=2026-08-01&to=2026-08-31");

        var cut = RenderComponent<NpsPage>();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".nps-result-row").Count));

        cut.Find("[data-sort='responses']").Click();
        Assert.Equal("ascending", cut.Find("th[data-column='responses']").GetAttribute("aria-sort"));
        Assert.Equal("Zulu", cut.FindAll(".nps-result-row .nps-result-project")[0].TextContent.Trim());

        cut.Find("[aria-label='Expandir respostas de Zulu']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".nps-result-expansion")));
        Assert.Contains("Resposta completa filtrada", cut.Find(".nps-result-expansion").TextContent);
        Assert.Contains(handler.RequestUris, uri => uri?.AbsolutePath == "/api/nps/projects/1/responses" && uri.Query.Contains("from=2026-08-01") && uri.Query.Contains("to=2026-08-31"));

        cut.Find("[aria-label='Expandir respostas de Alpha']").Click();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".nps-result-expansion")));
        Assert.Equal("2", cut.Find(".nps-result-expansion").GetAttribute("data-project-id"));

        cut.Find(".nps-results-table").KeyDown("Escape");
        Assert.Empty(cut.FindAll(".nps-result-expansion"));
    }

    [Fact]
    public void Result_expansion_failure_should_stay_in_the_row_and_retry_without_hiding_the_page()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/project-results", ProjectResultsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects/1/responses", "{}", HttpStatusCode.InternalServerError);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects/1/responses", FilteredResponsesJson(), HttpStatusCode.OK);
        Navigate("/nps/resultados");

        var cut = RenderComponent<NpsPage>();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".nps-result-row").Count));
        cut.Find("[aria-label='Expandir respostas de Zulu']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Não foi possível carregar as respostas deste projeto.", cut.Find(".nps-result-expansion").TextContent));
        Assert.Equal(4, cut.FindAll(".nps-indicators .stat").Count);
        Assert.Equal(2, cut.FindAll(".nps-result-row").Count);

        cut.Find(".nps-expansion-error button").Click();
        cut.WaitForAssertion(() => Assert.Contains("Resposta completa filtrada", cut.Find(".nps-result-expansion").TextContent));
    }

    [Fact]
    public void Responses_should_render_the_exact_placeholder_without_calling_a_global_query()
    {
        var handler = RegisterClient();
        Navigate("/nps/respostas?format=complete");

        var cut = RenderComponent<NpsPage>();

        Assert.Contains("A tabela de auditoria chega numa entrega seguinte.", cut.Markup);
        Assert.Equal("A tabela de auditoria chega numa entrega seguinte.", cut.Find(".nps-empty-delivery p").TextContent.Trim());
        Assert.Empty(cut.FindAll(".nps-indicators"));
        Assert.Empty(cut.FindAll(".nps-search"));
        Assert.Empty(cut.FindAll(".fmenu__btn"));
        Assert.Contains("nps-toolbar", cut.Find(".view-tabs").ParentElement!.ClassList);
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

        cut.WaitForAssertion(() =>
        {
            var dialog = Dialog(cut, "Gerar link NPS");
            Assert.Contains("Projeto, formato e idioma", dialog.TextContent);
            Assert.Equal(new[] { "Projeto", "Formato", "Idioma" }, dialog.QuerySelectorAll(".nps-field > span").Select(label => label.TextContent.Trim()));
            Assert.Equal(new[] { "Cancelar", "Gerar link" }, dialog.QuerySelectorAll(".brq-dialog-footer button").Select(button => button.TextContent.Trim()));
        });
        cut.Find(".nps-create-project").Change("1");
        cut.Find(".nps-create-submit").Click();

        cut.WaitForAssertion(() =>
        {
            var dialog = Dialog(cut, "Gerar link NPS");
            Assert.Contains(token.ToString(), dialog.InnerHtml);
            Assert.Contains("21/08/2026", dialog.TextContent);
            Assert.Equal(new[] { "Link", "Validade", "Mensagem sugerida" }, dialog.QuerySelectorAll(".nps-created-block > span").Select(label => label.TextContent.Trim()));
            Assert.Equal(new[] { "Copiar link", "Copiar mensagem" }, dialog.QuerySelectorAll(".brq-dialog-footer button").Select(button => button.TextContent.Trim()));
            Assert.Contains("Completo", dialog.TextContent);
        });
    }

    [Fact]
    public void Detail_dialog_should_render_compact_kpis_labeled_links_footer_and_segmented_response_filter()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", ProjectsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects/1", DetailJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects/1/responses", FilteredResponsesJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta");

        var cut = RenderComponent<NpsPage>();
        cut.WaitForAssertion(() => Assert.Contains("Projeto ativo", cut.Markup));
        cut.Find("[aria-label='Ver detalhe de Projeto ativo']").Click();

        cut.WaitForAssertion(() =>
        {
            var dialog = Dialog(cut, "Detalhe da coleta");
            Assert.Equal(4, dialog.QuerySelectorAll(".nps-detail-kpi").Length);
            Assert.Equal("Links ativos", dialog.QuerySelector(".nps-detail-links h3")!.TextContent.Trim());
            Assert.Contains("Formato", dialog.TextContent);
            Assert.Contains("Validade", dialog.TextContent);
            Assert.Equal(3, dialog.QuerySelectorAll(".nps-detail-segment").Length);
            Assert.Equal("true", dialog.QuerySelector("[data-format='all']")!.GetAttribute("aria-pressed"));
            Assert.Equal("Fechar", dialog.QuerySelector(".brq-dialog-footer button")!.TextContent.Trim());
        });

        Dialog(cut, "Detalhe da coleta").QuerySelector("[data-format='complete']")!.Click();

        cut.WaitForAssertion(() =>
        {
            var dialog = Dialog(cut, "Detalhe da coleta");
            Assert.Equal("true", dialog.QuerySelector("[data-format='complete']")!.GetAttribute("aria-pressed"));
            Assert.Contains("Resposta completa filtrada", dialog.TextContent);
            Assert.Contains(handler.RequestUris, uri => uri?.Query == "?format=complete");
        });
    }

    [Fact]
    public void Waiver_dialog_should_label_the_reason_and_keep_cancel_and_confirmation_in_the_footer()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", ProjectsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Post, "/api/nps/projects/1/waiver", DetailJson(), HttpStatusCode.Created);
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", ProjectsJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta");

        var cut = RenderComponent<NpsPage>();
        cut.WaitForAssertion(() => Assert.Contains("Projeto ativo", cut.Markup));
        cut.Find("[aria-label='Dispensar coleta de Projeto ativo']").Click();

        var dialog = Dialog(cut, "Dispensar coleta");
        Assert.Equal("Motivo da dispensa", dialog.QuerySelector(".nps-field > span")!.TextContent.Trim());
        Assert.Equal(new[] { "Cancelar", "Confirmar dispensa" }, dialog.QuerySelectorAll(".brq-dialog-footer button").Select(button => button.TextContent.Trim()));
        Assert.True(dialog.QuerySelector(".nps-waiver-confirm")!.HasAttribute("disabled"));

        dialog.QuerySelector(".nps-waiver-cancel")!.Click();
        Assert.Equal("false", Dialog(cut, "Dispensar coleta").GetAttribute("data-open"));

        cut.Find("[aria-label='Dispensar coleta de Projeto ativo']").Click();
        cut.Find(".nps-waiver-reason").Change("Contrato encerrado");
        Assert.False(Dialog(cut, "Dispensar coleta").QuerySelector(".nps-waiver-confirm")!.HasAttribute("disabled"));
        Dialog(cut, "Dispensar coleta").QuerySelector(".nps-waiver-confirm")!.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(handler.RequestUris, uri => uri?.AbsolutePath == "/api/nps/projects/1/waiver");
            Assert.Equal("false", Dialog(cut, "Dispensar coleta").GetAttribute("data-open"));
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

    private static AngleSharp.Dom.IElement Dialog(IRenderedComponent<NpsPage> cut, string title)
        => cut.FindAll("dialog").Single(dialog => dialog.TextContent.Contains(title, StringComparison.Ordinal));

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

    private static string DetailJson() => """
        {
          "project":{
            "id":1,"name":"Projeto ativo","client":"Alpha","dc":"DC1","deliveryManager":"Maria","projectType":"Squad","responsesCount":2,
            "stage":{"code":"awaiting_response","label":"Aguardando resposta","tone":"warning"},
            "temporal":{"label":"Enviado há 2d","tone":"warning","at":"2026-08-19T12:00:00Z"},"waiver":null,
            "activeLinks":[{"dispatchId":10,"token":"11111111-1111-1111-1111-111111111111","format":"complete","formatLabel":"Completo","expiresAt":"2026-08-21T12:00:00Z","availability":"open","availabilityLabel":"Aberto","tone":"positive"}],
            "primaryAction":{"code":"copy_link","label":"Copiar link","format":"complete","dispatchId":10,"token":"11111111-1111-1111-1111-111111111111"},
            "isOverdue":false,"lastDispatchClosedAt":null
          },
          "officialNps":50.0,"averageScore":8.5,"responsesCount":2,"promotersCount":1,
          "activeLinks":[{"dispatchId":10,"token":"11111111-1111-1111-1111-111111111111","format":"complete","formatLabel":"Completo","expiresAt":"2026-08-21T12:00:00Z","availability":"open","availabilityLabel":"Aberto","tone":"positive"}],
          "recentResponses":[{"id":1,"projectId":1,"projectName":"Projeto ativo","dispatchId":10,"targetId":20,"contactId":null,"contactName":null,"contactEmail":null,"format":"complete","formatLabel":"Completo","score":10,"classification":"promoter","classificationLabel":"Promotor","quality":5,"schedule":5,"communication":5,"businessValue":5,"comment":"Excelente parceria","respondentName":null,"respondentEmail":null,"submittedAt":"2026-08-20T12:00:00Z"}]
        }
        """;

    private static string FilteredResponsesJson() => """
        [
          {"id":2,"projectId":1,"projectName":"Projeto ativo","dispatchId":10,"targetId":21,"contactId":null,"contactName":null,"contactEmail":null,"format":"complete","formatLabel":"Completo","score":9,"classification":"promoter","classificationLabel":"Promotor","quality":5,"schedule":4,"communication":5,"businessValue":5,"comment":"Resposta completa filtrada","respondentName":null,"respondentEmail":null,"submittedAt":"2026-08-21T12:00:00Z"}
        ]
        """;

    private static string ProjectResultsJson() => """
        [
          {
            "id":1,"name":"Zulu","client":"Beta","dc":"DC1","deliveryManager":"Maria","responsesCount":1,"officialNps":100.0,
            "distribution":[{"code":"detractor","label":"Detrator","tone":"critical","count":0,"percentage":0.0},{"code":"passive","label":"Neutro","tone":"warning","count":0,"percentage":0.0},{"code":"promoter","label":"Promotor","tone":"positive","count":1,"percentage":100.0}],
            "formats":[{"code":"complete","label":"Completo","count":1},{"code":"simplified","label":"Simplificado","count":0}],
            "lastResponseAt":"2026-08-21T12:00:00Z","status":{"code":"responded","label":"Respondido","tone":"positive"}
          },
          {
            "id":2,"name":"Alpha","client":"Alpha","dc":"DC2","deliveryManager":"João","responsesCount":2,"officialNps":0.0,
            "distribution":[{"code":"detractor","label":"Detrator","tone":"critical","count":1,"percentage":50.0},{"code":"passive","label":"Neutro","tone":"warning","count":0,"percentage":0.0},{"code":"promoter","label":"Promotor","tone":"positive","count":1,"percentage":50.0}],
            "formats":[{"code":"complete","label":"Completo","count":1},{"code":"simplified","label":"Simplificado","count":1}],
            "lastResponseAt":"2026-08-20T12:00:00Z","status":{"code":"responded","label":"Respondido","tone":"positive"}
          }
        ]
        """;
}
