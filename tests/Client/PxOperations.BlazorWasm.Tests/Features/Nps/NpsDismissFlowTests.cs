using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

/// <summary>
/// F6 de ponta a ponta pela tela: menu do card → motivo → confirmação → POST.
/// O caminho da confirmação não tinha teste, e uma refatoração o deixou mudo:
/// o motivo digitado no diálogo se perdia e nenhuma requisição saía.
/// </summary>
public sealed class NpsDismissFlowTests : TestContext
{
    public NpsDismissFlowTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Confirming_the_dialog_should_send_the_typed_reason()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, ProjectsJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Post, ProjectsJson().Trim('[', ']', '\n', ' '), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, "[]", HttpStatusCode.OK);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<NpsClient>();

        var cut = RenderComponent<NpsPage>(p => p.Add(x => x.Tab, "coleta"));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".kcard")));

        cut.Find(".kcard__kebab").Click();
        cut.Find(".kcard__menu-item").Click();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("#dismiss-reason")));
        cut.Find("#dismiss-reason").Change("Cliente pediu pausa");
        cut.FindAll(".nps-inline-form__actions button").Single(b => b.TextContent.Contains("Dispensar")).Click();

        cut.WaitForAssertion(() => Assert.Contains(
            handler.RequestUris,
            uri => uri!.AbsolutePath.EndsWith("/collection-waiver")));

        // O motivo tem de chegar ao servidor: contar requisições não basta,
        // passa com o payload vazio.
        var indice = handler.RequestUris.FindIndex(u => u!.AbsolutePath.EndsWith("/collection-waiver"));
        Assert.Contains("Cliente pediu pausa", handler.RequestBodies[indice]);
    }

    /// <summary>F6 exige motivo: confirmar vazio avisa e não envia nada.</summary>
    [Fact]
    public void Confirming_without_a_reason_should_warn_and_not_post()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, DashboardJson(), HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Get, ProjectsJson(), HttpStatusCode.OK);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<NpsClient>();

        var cut = RenderComponent<NpsPage>(p => p.Add(x => x.Tab, "coleta"));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".kcard")));

        cut.Find(".kcard__kebab").Click();
        cut.Find(".kcard__menu-item").Click();
        cut.FindAll(".nps-inline-form__actions button").Single(b => b.TextContent.Contains("Dispensar")).Click();

        cut.WaitForAssertion(() => Assert.Contains("Informe o motivo", cut.Find(".nps-error-text").TextContent));
        Assert.DoesNotContain(handler.RequestUris, uri => uri!.AbsolutePath.EndsWith("/collection-waiver"));
    }

    private static string DashboardJson() => """
    {"totalProjects":1,"overdueProjects":0,"activeDispatches":1,"totalResponses":0,"officialNps":0.0,"averageScore":0.0,"detractors":0,"passives":0,"promoters":0}
    """;

    private static string ProjectsJson() => """
    [{"id":1,"name":"Projeto NPS","client":"Cliente A","dc":"DC1","deliveryManager":"Maria","contactsCount":0,"activeDispatches":1,"linkTargetsCount":1,"answeredLinkTargetsCount":0,"responsesCount":0,"lastResponseAt":null,"lastNps":null,"isOverdue":false,"collectionStatus":"Pendente","isDismissed":false,"dismissalReason":null,"activeDispatchExpiresAt":"2026-09-15T00:00:00Z"}]
    """;
}
