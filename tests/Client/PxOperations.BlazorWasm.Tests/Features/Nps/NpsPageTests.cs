using System.Globalization;
using System.Net;
using Bunit;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

public sealed class NpsPageTests : BunitContext
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

        Render<NpsPage>();

        Assert.EndsWith("/nps/coleta?client=Alpha", Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Empty(handler.RequestUris);
    }

    [Fact]
    public void Collection_should_keep_four_columns_and_render_waived_projects_below_them()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta?includeWaived=true");

        var cut = Render<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Single(cut.FindAll("h1"));
            Assert.Equal("NPS", cut.Find("h1").TextContent.Trim());
            Assert.Equal(4, cut.FindAll(".nps-indicators .stat").Count);
            Assert.Equal(4, cut.FindAll(".nps-board-column").Count);
            Assert.Empty(cut.FindAll(".nps-aspect-summary"));
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
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta");

        var cut = Render<NpsPage>();

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
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta");

        var cut = Render<NpsPage>();

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
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta");

        var cut = Render<NpsPage>();

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
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta?client=Alpha&client=Beta&format=complete");

        var cut = Render<NpsPage>();
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
    public void Results_should_render_distribution_and_accessible_aspect_averages_side_by_side()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/project-results", NpsTestHelpers.ProjectResultsJson(), HttpStatusCode.OK);
        Navigate("/nps/resultados");

        var cut = Render<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(4, cut.FindAll(".nps-indicators .stat").Count);
            Assert.Empty(cut.FindAll(".nps-results .nps-kpi"));
            var executivePanels = cut.Find(".nps-executive-panels");
            Assert.Equal(new[] { "nps-results", "nps-aspect-summary" }, executivePanels.Children.Select(child => child.ClassName));
            Assert.Equal("Distribuição das respostas", cut.Find(".nps-results h2").TextContent.Trim());
            Assert.Contains("% promotores − % detratores", cut.Markup);
            var items = cut.FindAll(".nps-distribution-legend li");
            Assert.Equal(new[] { "Detrator", "Neutro", "Promotor" }, items.Select(item => item.QuerySelector("strong")!.TextContent));
            Assert.Equal(3, cut.FindAll(".nps-legend-swatch").Count);
            var aspectSummary = cut.Find(".nps-aspect-summary");
            Assert.Equal("Médias por aspecto", aspectSummary.QuerySelector("h2")!.TextContent.Trim());
            Assert.Contains("4 respostas Completas · escala 1–5", aspectSummary.TextContent);
            Assert.Equal(
                new[] { "Qualidade técnica", "Prazos acordados", "Comunicação", "Valor para o negócio" },
                aspectSummary.QuerySelectorAll(".nps-aspect-label").Select(label => label.TextContent.Trim()));
            Assert.Equal(new[] { "4,2", "3,5", "4,0", "—" }, aspectSummary.QuerySelectorAll(".nps-aspect-average").Select(value => value.TextContent.Trim()));
            Assert.Equal(new[] { "n=3", "n=2", "n=4", "n=0" }, aspectSummary.QuerySelectorAll(".nps-aspect-count").Select(value => value.TextContent.Trim()));
            var meters = aspectSummary.QuerySelectorAll("meter");
            Assert.Equal(3, meters.Length);
            Assert.All(meters, meter =>
            {
                Assert.Equal("1", meter.GetAttribute("min"));
                Assert.Equal("5", meter.GetAttribute("max"));
                Assert.False(string.IsNullOrWhiteSpace(meter.GetAttribute("value")));
                Assert.Contains("média", meter.GetAttribute("aria-label"));
                Assert.Contains("respostas", meter.GetAttribute("aria-label"));
            });
            Assert.Contains("Sem respostas para Valor para o negócio", aspectSummary.QuerySelector(".nps-aspect-meter-empty")!.GetAttribute("aria-label"));
            var headers = cut.FindAll(".nps-results-table thead th");
            Assert.Equal(new[] { "Projeto", "Cliente", "DC", "DM", "Respostas", "NPS", "Status" }, headers.Select(header => header.TextContent.Trim()));
            Assert.Equal("ascending", headers[0].GetAttribute("aria-sort"));
            Assert.Equal(new[] { "Alpha", "Zulu" }, cut.FindAll(".nps-result-row .nps-result-project").Select(cell => cell.TextContent.Trim()));
        });
    }

    [Fact]
    public void Results_should_show_a_localized_empty_state_without_complete_responses()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardWithoutCompleteResponsesJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/project-results", "[]", HttpStatusCode.OK);
        Navigate("/nps/resultados");

        var cut = Render<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            var summary = cut.Find(".nps-aspect-summary");
            Assert.Contains("Nenhuma resposta Completa no recorte atual.", summary.TextContent);
            Assert.Empty(summary.QuerySelectorAll(".nps-aspect-row"));
        });
    }

    [Fact]
    public void Tabs_should_switch_the_rendered_view_without_reloading_the_page()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/project-results", NpsTestHelpers.ProjectResultsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        Navigate("/nps/resultados");

        var cut = Render<NpsPage>();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".nps-aspect-summary")));

        cut.Find("a[href='/nps/coleta']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.EndsWith("/nps/coleta", Services.GetRequiredService<NavigationManager>().Uri);
            Assert.Equal(4, cut.FindAll(".nps-board-column").Count);
            Assert.Empty(cut.FindAll(".nps-aspect-summary"));
        });
    }

    [Fact]
    public void Results_should_sort_accessibly_and_keep_only_one_lazy_expansion_open()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/project-results", NpsTestHelpers.ProjectResultsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects/1/responses", NpsTestHelpers.FilteredResponsesJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects/2/responses", NpsTestHelpers.FilteredResponsesJson(), HttpStatusCode.OK);
        Navigate("/nps/resultados?from=2026-08-01&to=2026-08-31");

        var cut = Render<NpsPage>();
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
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/project-results", NpsTestHelpers.ProjectResultsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects/1/responses", "{}", HttpStatusCode.InternalServerError);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects/1/responses", NpsTestHelpers.FilteredResponsesJson(), HttpStatusCode.OK);
        Navigate("/nps/resultados");

        var cut = Render<NpsPage>();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".nps-result-row").Count));
        cut.Find("[aria-label='Expandir respostas de Zulu']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Não foi possível carregar as respostas deste projeto.", cut.Find(".nps-result-expansion").TextContent));
        Assert.Equal(4, cut.FindAll(".nps-indicators .stat").Count);
        Assert.Equal(2, cut.FindAll(".nps-result-row").Count);

        cut.Find(".nps-expansion-error button").Click();
        cut.WaitForAssertion(() => Assert.Contains("Resposta completa filtrada", cut.Find(".nps-result-expansion").TextContent));
    }

    [Fact]
    public void Responses_should_load_options_and_audit_in_parallel_without_indicators()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/filter-options", NpsTestHelpers.FilterOptionsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/responses", NpsTestHelpers.ResponsesJson(), HttpStatusCode.OK);
        Navigate("/nps/respostas?format=complete&status=responded");

        var cut = Render<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".nps-indicators"));
            Assert.Empty(cut.FindAll(".nps-aspect-summary"));
            Assert.Equal("Buscar projeto, pessoa ou comentário", cut.Find(".nps-search input").GetAttribute("placeholder"));
            Assert.Single(cut.FindAll(".fmenu__btn"));
            Assert.Equal(new[] { "Projeto", "Nota", "Classificação", "Formato", "Autor", "Comentário", "Recebida" }, cut.FindAll(".nps-responses-table th").Select(header => header.TextContent.Trim()));
            Assert.Equal(2, cut.FindAll(".nps-response-row").Count);
        });
        Assert.Contains(handler.RequestUris, uri => uri?.AbsolutePath == "/api/nps/filter-options");
        Assert.Contains(handler.RequestUris, uri => uri?.AbsolutePath == "/api/nps/responses" && uri.Query.Contains("format=complete") && !uri.Query.Contains("status="));
        Assert.DoesNotContain("status=", cut.Find("a[href*='responses/export']").GetAttribute("href"));
    }

    [Fact]
    public void Response_dialog_should_show_full_attribution_and_aspects_only_for_complete_format()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/filter-options", NpsTestHelpers.FilterOptionsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/responses", NpsTestHelpers.ResponsesJson(), HttpStatusCode.OK);
        Navigate("/nps/respostas");

        var cut = Render<NpsPage>();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".nps-response-row").Count));
        Assert.Equal("Resposta anônima", cut.FindAll(".nps-response-author")[1].TextContent.Trim());
        Assert.Equal("Comentário completo que deve permanecer acessível", cut.FindAll(".nps-response-comment")[0].GetAttribute("title"));

        cut.FindAll(".nps-response-row")[0].Click();
        var completeDialog = Dialog(cut, "Resposta NPS");
        Assert.Contains("Pessoa Teste", completeDialog.TextContent);
        Assert.Contains("Comentário completo que deve permanecer acessível", completeDialog.TextContent);
        Assert.Equal(new[] { "Qualidade", "Prazo", "Comunicação", "Valor para o negócio" }, completeDialog.QuerySelectorAll(".nps-response-aspect dt").Select(item => item.TextContent.Trim()));
        Assert.Contains("Média dos aspectos", completeDialog.TextContent);
        Assert.Contains("3,5", completeDialog.TextContent);

        completeDialog.QuerySelector(".brq-dialog-footer button")!.Click();
        cut.FindAll(".nps-response-row")[1].KeyDown("Enter");
        var simplifiedDialog = Dialog(cut, "Resposta NPS");
        Assert.Contains("Resposta anônima", simplifiedDialog.TextContent);
        Assert.Empty(simplifiedDialog.QuerySelectorAll(".nps-response-aspects"));
    }

    [Fact]
    public void Generate_link_should_have_two_steps_and_use_server_expiration_for_the_message()
    {
        var token = Guid.NewGuid();
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Post, "/api/nps/dispatches", NpsTestHelpers.DispatchJson(token), HttpStatusCode.Created);
        Navigate("/nps/coleta");

        var cut = Render<NpsPage>();
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
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects/1", NpsTestHelpers.DetailJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects/1/responses", NpsTestHelpers.FilteredResponsesJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta");

        var cut = Render<NpsPage>();
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
            Assert.Contains(handler.RequestUris, uri =>
                uri?.AbsolutePath == "/api/nps/projects/1/responses" &&
                uri.Query.Contains("format=complete", StringComparison.Ordinal) &&
                uri.Query.Contains("includeWaived=true", StringComparison.Ordinal));
        });
    }

    [Fact]
    public void Waiver_dialog_should_label_the_reason_and_keep_cancel_and_confirmation_in_the_footer()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Post, "/api/nps/projects/1/waiver", NpsTestHelpers.DetailJson(), HttpStatusCode.Created);
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta");

        var cut = Render<NpsPage>();
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

        var cut = Render<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(4, cut.FindAll(".nps-board-column").Count);
            Assert.Contains("Tentar novamente", cut.Markup);
            Assert.DoesNotContain("Projeto ativo", cut.Markup);
        });
    }

    [Fact]
    public void Distribution_bar_should_size_segments_with_an_invariant_decimal_separator()
    {
        var original = CultureInfo.CurrentCulture;
        var originalDefault = CultureInfo.DefaultThreadCurrentCulture;
        var brazilian = CultureInfo.GetCultureInfo("pt-BR");
        CultureInfo.CurrentCulture = brazilian;
        CultureInfo.DefaultThreadCurrentCulture = brazilian;
        try
        {
            var handler = RegisterClient();
            handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.FractionalDashboardJson(), HttpStatusCode.OK);
            handler.AddResponse(HttpMethod.Get, "/api/nps/project-results", NpsTestHelpers.ProjectResultsJson(), HttpStatusCode.OK);
            Navigate("/nps/resultados");

            var cut = Render<NpsPage>();

            cut.WaitForAssertion(() =>
            {
                var widths = cut.FindAll(".nps-distribution-bar span")
                    .Select(span => span.GetAttribute("style") ?? string.Empty)
                    .ToArray();
                Assert.Contains("width:33.333%", widths);
                Assert.DoesNotContain(widths, style => style.Contains(',', StringComparison.Ordinal));
            });
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.DefaultThreadCurrentCulture = originalDefault;
        }
    }

    [Fact]
    public void Results_should_apply_the_waived_filter_to_the_table_and_the_indicators()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/project-results", NpsTestHelpers.ProjectResultsJson(), HttpStatusCode.OK);
        Navigate("/nps/resultados?includeWaived=true");

        var cut = Render<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            var results = handler.RequestUris
                .Single(uri => uri!.AbsolutePath == "/api/nps/project-results");
            Assert.Contains("includeWaived=true", results!.Query, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Generating_a_link_should_refresh_the_board()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Post, "/api/nps/dispatches", NpsTestHelpers.DispatchJson(Guid.NewGuid()), HttpStatusCode.Created);
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta");

        var cut = Render<NpsPage>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".nps-primary-action")));

        cut.Find(".nps-primary-action").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".nps-create-submit")));
        cut.Find(".nps-create-submit").Click();

        cut.WaitForAssertion(() => Assert.Equal(
            2,
            handler.RequestUris.Count(uri => uri!.AbsolutePath == "/api/nps/projects")));
    }

    [Fact]
    public void Failed_link_generation_should_report_the_reason_the_server_gave()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        handler.AddResponse(
            HttpMethod.Post,
            "/api/nps/dispatches",
            """{"detail":"Waived NPS collections cannot create dispatches."}""",
            HttpStatusCode.Conflict);
        Navigate("/nps/coleta");

        var cut = Render<NpsPage>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".nps-primary-action")));

        cut.Find(".nps-primary-action").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".nps-create-submit")));
        cut.Find(".nps-create-submit").Click();

        cut.WaitForAssertion(() => Assert.Contains(
            "Waived NPS collections cannot create dispatches.",
            Dialog(cut, "Gerar link NPS").TextContent,
            StringComparison.Ordinal));
    }

    [Fact]
    public void Failed_waiver_should_report_the_reason_the_server_gave()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        handler.AddResponse(
            HttpMethod.Post,
            "/api/nps/projects/1/waiver",
            """{"detail":"NPS collection is already waived."}""",
            HttpStatusCode.Conflict);
        Navigate("/nps/coleta");

        var cut = Render<NpsPage>();
        cut.WaitForAssertion(() => Assert.Contains("Projeto ativo", cut.Markup));

        cut.Find("[aria-label='Dispensar coleta de Projeto ativo']").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".nps-waiver-reason")));
        cut.Find(".nps-waiver-reason").Change("Cliente pediu pausa");
        cut.Find(".nps-waiver-confirm").Click();

        cut.WaitForAssertion(() => Assert.Contains(
            "NPS collection is already waived.",
            Dialog(cut, "Dispensar coleta").TextContent,
            StringComparison.Ordinal));
    }

    [Fact]
    public void Clipboard_failure_should_be_reported_instead_of_failing_silently()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Post, "/api/nps/dispatches", NpsTestHelpers.DispatchJson(Guid.NewGuid()), HttpStatusCode.Created);
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        JSInterop.SetupVoid(invocation => invocation.Identifier == "navigator.clipboard.writeText")
            .SetException(new JSException("Clipboard indisponível."));
        Navigate("/nps/coleta");

        var cut = Render<NpsPage>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".nps-primary-action")));

        cut.Find(".nps-primary-action").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".nps-create-submit")));
        cut.Find(".nps-create-submit").Click();
        cut.WaitForAssertion(() => Assert.Contains(
            "Copiar link",
            Dialog(cut, "Gerar link NPS").TextContent,
            StringComparison.Ordinal));

        Dialog(cut, "Gerar link NPS")
            .QuerySelectorAll(".brq-dialog-footer button")
            .Single(button => button.TextContent.Trim() == "Copiar link")
            .Click();

        cut.WaitForAssertion(() => Assert.Contains(
            "Não foi possível copiar",
            Dialog(cut, "Gerar link NPS").TextContent,
            StringComparison.Ordinal));
    }

    [Fact]
    public void Router_reentry_after_a_filter_change_should_not_load_the_same_page_twice()
    {
        var handler = RegisterClient();
        for (var round = 0; round < 4; round++)
        {
            handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
            handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        }

        Navigate("/nps/coleta");
        var cut = Render<NpsPage>();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".nps-board-column")));

        cut.Find(".nps-toolbar .fmenu__btn").Click();
        cut.FindAll(".fmenu__pop input[type=checkbox]").Last().Change(true);
        cut.WaitForAssertion(() => Assert.Equal(
            2,
            handler.RequestUris.Count(uri => uri!.AbsolutePath == "/api/nps/projects")));

        // O NavigateTo do filtro dispara LocationChanged, o Router repassa os
        // parâmetros e OnParametersSetAsync roda de novo para a mesma rota.
        cut.Render(ParameterView.Empty);

        cut.WaitForAssertion(() => Assert.Equal(
            2,
            handler.RequestUris.Count(uri => uri!.AbsolutePath == "/api/nps/projects")));
    }

    [Fact]
    public void Response_timestamps_should_be_shown_in_the_operation_timezone()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/filter-options", NpsTestHelpers.FilterOptionsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/responses", NpsTestHelpers.ResponsesJson(), HttpStatusCode.OK);
        Navigate("/nps/respostas");

        var cut = Render<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            var received = cut.FindAll(".nps-response-row td time").Select(cell => cell.TextContent.Trim()).ToArray();
            // O payload traz 2026-08-21T12:00:00Z, que na operação é 09:00.
            Assert.Contains("21/08/2026 09:00", received);
            Assert.DoesNotContain("21/08/2026 12:00", received);
        });
    }

    [Fact]
    public void Response_row_should_take_the_tone_the_server_sent()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/filter-options", NpsTestHelpers.FilterOptionsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/responses", NpsTestHelpers.ResponsesJson(), HttpStatusCode.OK);
        Navigate("/nps/respostas");

        var cut = Render<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            // A primeira resposta é "promoter" mas o servidor mandou tom de
            // atenção: derivar da classificação daria "good" e o teste cairia.
            var pill = cut.FindAll(".nps-response-row td .pill")[0];
            Assert.Contains("pill--warn", pill.ClassList);
        });
    }

    /// <summary>
    /// O detalhe é aberto com o projeto dispensado incluído; filtrar por formato
    /// dentro dele não pode aplicar um recorte que o próprio modal ignora, senão
    /// a lista esvazia e não volta.
    /// </summary>
    [Fact]
    public void Filtering_the_detail_by_format_should_keep_waived_projects_in_scope()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/dashboard", NpsTestHelpers.DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects", NpsTestHelpers.ProjectsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects/1", NpsTestHelpers.DetailJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/projects/1/responses", NpsTestHelpers.FilteredResponsesJson(), HttpStatusCode.OK);
        Navigate("/nps/coleta?includeWaived=true");

        var cut = Render<NpsPage>();
        cut.WaitForAssertion(() => Assert.Contains("Projeto ativo", cut.Markup));
        cut.Find("[aria-label='Ver detalhe de Projeto ativo']").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-format='complete']")));

        cut.Find("[data-format='complete']").Click();

        cut.WaitForAssertion(() =>
        {
            var request = handler.RequestUris.Last(uri => uri!.AbsolutePath == "/api/nps/projects/1/responses");
            Assert.Contains("includeWaived=true", request!.Query, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Responses_tab_and_export_should_carry_the_waived_filter()
    {
        var handler = RegisterClient();
        handler.AddResponse(HttpMethod.Get, "/api/nps/filter-options", NpsTestHelpers.FilterOptionsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "/api/nps/responses", NpsTestHelpers.ResponsesJson(), HttpStatusCode.OK);
        Navigate("/nps/respostas?includeWaived=true");

        var cut = Render<NpsPage>();

        cut.WaitForAssertion(() =>
        {
            var request = handler.RequestUris.Single(uri => uri!.AbsolutePath == "/api/nps/responses");
            Assert.Contains("includeWaived=true", request!.Query, StringComparison.Ordinal);
        });
        Assert.Contains("includeWaived=true", cut.Find("a.btn-ghost").GetAttribute("href")!, StringComparison.Ordinal);
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
}
