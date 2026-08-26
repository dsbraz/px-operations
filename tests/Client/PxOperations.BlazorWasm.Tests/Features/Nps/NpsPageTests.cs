using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

public sealed class NpsPageTests : TestContext
{
    public NpsPageTests()
    {
        // A barra de filtros usa BrqFilterPanel, que importa um módulo JS para
        // fechar ao clicar fora, e o clipboard no compartilhamento do link.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void NpsPage_should_render_dashboard_and_projects()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, ProjectsJson(), HttpStatusCode.OK);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<NpsClient>();

        var cut = RenderComponent<NpsPage>(p => p.Add(x => x.Tab, "resultados"));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("NPS oficial", cut.Markup);
            Assert.Contains("Projeto NPS", cut.Markup);
            Assert.Contains("Status NPS", cut.Markup);
            Assert.Contains("Link NPS", cut.Markup);
            Assert.Contains("Última resposta", cut.Markup);
            Assert.Contains("Pendente", cut.Markup);
            Assert.Contains("Sem link", cut.Markup);
        });
    }

    [Fact]
    public void Export_link_should_use_configured_api_base_address()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, ProjectsJson(), HttpStatusCode.OK);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8081/") };
        Services.AddScoped(_ => client);
        Services.AddScoped<NpsClient>();

        var cut = RenderComponent<NpsPage>(p => p.Add(x => x.Tab, "resultados"));

        cut.WaitForAssertion(() =>
        {
            var export = cut.Find("a[href*='api/nps/responses/export']");
            Assert.Equal("http://localhost:8081/api/nps/responses/export", export.GetAttribute("href"));
        });
    }

    [Fact]
    public void Generate_link_should_open_modal_and_allow_project_selection()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, ProjectsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, ProjectDetailJson(), HttpStatusCode.OK);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<NpsClient>();

        var cut = RenderComponent<NpsPage>(p => p.Add(x => x.Tab, "resultados"));
        cut.WaitForAssertion(() => Assert.Contains("Projeto NPS", cut.Markup));

        var generateLinkButton = cut.Find(".page-head__actions button");
        Assert.Equal("Gerar link", generateLinkButton.TextContent.Trim());
        Assert.Null(generateLinkButton.GetAttribute("disabled"));

        generateLinkButton.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Novo link NPS", cut.Markup);
            Assert.Contains("Selecione o projeto", cut.Markup);
        });

        cut.Find(".modal select").Change("1");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Projeto NPS · DC1", cut.Markup);
            Assert.Contains("Gerar link", cut.Markup);
            Assert.DoesNotContain("Destinatários", cut.Markup);
            Assert.Empty(cut.FindAll(".modal input"));
        });
    }

    [Fact]
    public void Project_history_action_should_open_detail_modal()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, ProjectsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, ProjectDetailJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, DispatchDetailJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, SurveyResponsesJson(), HttpStatusCode.OK);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<NpsClient>();

        var cut = RenderComponent<NpsPage>(p => p.Add(x => x.Tab, "resultados"));
        cut.WaitForAssertion(() => Assert.Contains("Projeto NPS", cut.Markup));

        cut.FindAll(".nps-row-actions button").Single(button => button.TextContent.Contains("Ver histórico")).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Histórico de links", cut.Markup);
            Assert.Contains("Respostas", cut.Markup);
            Assert.Contains("Respondido", cut.Find(".nps-history-card").TextContent);
            Assert.DoesNotContain("1 respostas", cut.Find(".nps-history-card").TextContent);
            Assert.Contains("Promotor", cut.Markup);
            Assert.Contains("cliente@example.com", cut.Markup);
            Assert.Contains("Comentário", cut.Markup);
            Assert.Contains("Valor para o negócio", cut.Markup);
            Assert.Contains("5/5", cut.Markup);
            Assert.Contains("Prazo", cut.Markup);
            Assert.Contains("4/5", cut.Markup);
            Assert.Contains("Qualidade", cut.Markup);
            Assert.Contains("Comunicação", cut.Markup);
            Assert.Contains("Bom", cut.Markup);
            Assert.DoesNotContain("Link selecionado", cut.Markup);
        });

        cut.Find(".nps-history-card").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Link selecionado", cut.Markup);
            Assert.Contains("Respondido", cut.Find(".nps-share-panel").TextContent);
            Assert.DoesNotContain("11111111-1111-1111-1111-111111111111", cut.Find(".nps-share-panel").TextContent);
            Assert.DoesNotContain("Copiar", cut.Find(".nps-share-panel").TextContent);
            Assert.Contains("Respostas deste link", cut.Markup);
            Assert.Contains("Respondente não identificado", cut.Markup);
            Assert.Contains("5/5", cut.Markup);
        });
    }

    [Fact]
    public void Creating_link_should_preserve_active_combo_filters_when_reloading_dashboard_and_projects()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, DashboardJson(), HttpStatusCode.OK); // initial dashboard
        handler.AddResponse(HttpMethod.Get, ProjectsJson(), HttpStatusCode.OK); // initial projects
        handler.AddResponse(HttpMethod.Get, DashboardJson(), HttpStatusCode.OK); // DC filtered dashboard
        handler.AddResponse(HttpMethod.Get, ProjectsJson(), HttpStatusCode.OK); // DC filtered projects
        handler.AddResponse(HttpMethod.Get, DashboardJson(), HttpStatusCode.OK); // type filtered dashboard
        handler.AddResponse(HttpMethod.Get, ProjectsJson(), HttpStatusCode.OK); // type filtered projects
        handler.AddResponse(HttpMethod.Get, ProjectDetailJson(), HttpStatusCode.OK); // select project
        handler.AddResponse(HttpMethod.Post, CreatedDispatchDetailJson(), HttpStatusCode.Created); // create dispatch
        handler.AddResponse(HttpMethod.Get, ProjectDetailJson(), HttpStatusCode.OK); // reselect project
        handler.AddResponse(HttpMethod.Get, DashboardJson(), HttpStatusCode.OK); // post-create dashboard
        handler.AddResponse(HttpMethod.Get, ProjectsJson(), HttpStatusCode.OK); // post-create projects

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<NpsClient>();

        var cut = RenderComponent<NpsPage>(p => p.Add(x => x.Tab, "resultados"));
        cut.WaitForAssertion(() => Assert.Contains("Projeto NPS", cut.Markup));

        // D11: os dropdowns viraram facetas dentro do menu do botão "Filtro".
        cut.Find("button.fmenu__btn").Click();
        cut.FindAll(".fmenu__pop fieldset")[0].QuerySelectorAll("input[type=checkbox]")[0].Change(true);
        cut.WaitForAssertion(() => Assert.Contains(handler.RequestUris, uri =>
            uri is not null
            && uri.AbsolutePath == "/api/nps/dashboard"
            && uri.Query.Contains("dc=DC1")));

        cut.FindAll(".fmenu__pop fieldset")[1].QuerySelectorAll("input[type=checkbox]")[0].Change(true);
        cut.WaitForAssertion(() => Assert.Contains(handler.RequestUris, uri =>
            uri is not null
            && uri.AbsolutePath == "/api/nps/dashboard"
            && uri.Query.Contains("dc=DC1")
            && uri.Query.Contains("projectType=Squad")));

        cut.FindAll(".nps-row-actions button").Single(button => button.TextContent.Contains("Gerar link")).Click();
        cut.WaitForAssertion(() => Assert.Contains("Novo link NPS", cut.Markup));

        cut.FindAll(".modal button").Single(button => button.TextContent.Contains("Gerar link")).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(handler.RequestUris, uri =>
                uri is not null
                && uri.AbsolutePath == "/api/nps/dashboard"
                && uri.Query.Contains("dc=DC1")
                && uri.Query.Contains("projectType=Squad"));
            // F3: o modal não fecha — passa ao passo 2, que entrega a URL, a
            // validade em destaque e a mensagem pronta para colar.
            Assert.Contains("11111111-1111-1111-1111-111111111111", cut.Find(".link-result__url").TextContent);
            Assert.Contains("Este link vale por 20 dias", cut.Find(".link-expiry").TextContent);

            var mensagem = cut.Find(".msg__text").TextContent;
            Assert.Contains("Estamos coletando o NPS do projeto", mensagem);
            Assert.Contains("11111111-1111-1111-1111-111111111111", mensagem);
            Assert.Contains("A pesquisa fica aberta até", mensagem);
        });

        JSInterop.SetupVoid("navigator.clipboard.writeText", _ => true);
        cut.FindAll(".msg__head button").Single(b => b.TextContent.Contains("Copiar mensagem")).Click();
        JSInterop.VerifyInvoke("navigator.clipboard.writeText");
    }

    private static string DashboardJson() => """
    {"totalProjects":1,"overdueProjects":1,"activeDispatches":0,"totalResponses":2,"officialNps":50.0,"averageScore":8.5,"detractors":0,"passives":1,"promoters":1}
    """;

    private static string ProjectsJson() => """
    [{"id":1,"name":"Projeto NPS","client":"Cliente A","dc":"DC1","deliveryManager":"Maria","contactsCount":1,"activeDispatches":0,"linkTargetsCount":0,"answeredLinkTargetsCount":0,"responsesCount":0,"lastResponseAt":null,"lastNps":null,"isOverdue":true,"isDismissed":false,"dismissalReason":null,"activeDispatchExpiresAt":null}]
    """;

    private static string ProjectDetailJson() => """
    {
      "project":{"id":1,"name":"Projeto NPS","client":"Cliente A","dc":"DC1","deliveryManager":"Maria","contactsCount":1,"activeDispatches":1,"linkTargetsCount":1,"answeredLinkTargetsCount":1,"responsesCount":1,"lastResponseAt":null,"lastNps":50.0,"isOverdue":false,"isDismissed":false,"dismissalReason":null,"activeDispatchExpiresAt":null},
      "contacts":[],
      "dispatches":[{"id":20,"projectId":1,"projectName":"Projeto NPS","periodStart":"2026-06-01","periodEnd":"2026-06-30","format":"Simplificado","language":"Português","status":"Aberto","createdBy":"ops","createdAt":"2026-06-01T00:00:00Z","closedAt":null,"expiresAt":"2026-06-21T00:00:00Z","isExpired":false,"targetsCount":1,"responsesCount":1}],
      "recentResponses":[{"id":30,"projectId":1,"projectName":"Projeto NPS","dispatchId":20,"targetId":40,"contactId":null,"contactName":null,"contactEmail":null,"score":9,"classification":"Promotor","businessValue":5,"schedule":4,"quality":5,"communication":5,"tags":null,"comment":"Bom","respondentName":"Cliente","respondentEmail":"cliente@example.com","submittedAt":"2026-06-02T00:00:00Z"}]
    }
    """;

    private static string DispatchDetailJson() => """
    {
      "dispatch":{"id":21,"projectId":1,"projectName":"Projeto NPS","periodStart":"2026-06-01","periodEnd":"2026-06-30","format":"Simplificado","language":"Português","status":"Aberto","createdBy":"ops","createdAt":"2026-06-01T00:00:00Z","closedAt":null,"expiresAt":"2026-06-21T00:00:00Z","isExpired":false,"targetsCount":1,"responsesCount":0},
      "targets":[{"id":41,"dispatchId":21,"contactId":null,"contactName":null,"contactEmail":null,"token":"11111111-1111-1111-1111-111111111111","isGeneric":true,"responsesCount":1}]
    }
    """;

    private static string CreatedDispatchDetailJson() => """
    {
      "dispatch":{"id":21,"projectId":1,"projectName":"Projeto NPS","periodStart":"2026-06-01","periodEnd":"2026-06-30","format":"Simplificado","language":"Português","status":"Aberto","createdBy":"ops","createdAt":"2026-06-01T00:00:00Z","closedAt":null,"expiresAt":"2026-06-21T00:00:00Z","isExpired":false,"targetsCount":1,"responsesCount":0},
      "targets":[{"id":41,"dispatchId":21,"contactId":null,"contactName":null,"contactEmail":null,"token":"11111111-1111-1111-1111-111111111111","isGeneric":true,"responsesCount":0}]
    }
    """;

    private static string SurveyResponseJson() => """
    {"id":30,"projectId":1,"projectName":"Projeto Público","dispatchId":2,"targetId":3,"contactId":null,"contactName":null,"contactEmail":null,"score":10,"classification":"Promotor","businessValue":5,"schedule":4,"quality":5,"communication":5,"tags":null,"comment":null,"respondentName":null,"respondentEmail":null,"submittedAt":"2026-06-02T00:00:00Z"}
    """;

    private static string SurveyResponsesJson()
        => $"[{SurveyResponseJson()}]";
}
