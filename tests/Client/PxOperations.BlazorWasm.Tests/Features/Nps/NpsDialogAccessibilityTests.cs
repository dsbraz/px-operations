using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

/// <summary>
/// O PRD pede navegação por teclado e estados ARIA junto com o tema. Os três
/// diálogos do NPS fecham só com o mouse e não se anunciam como diálogo — para
/// um leitor de tela eram conteúdo comum no meio da página.
/// </summary>
public sealed class NpsDialogAccessibilityTests : TestContext
{
    public NpsDialogAccessibilityTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void The_dismiss_dialog_should_announce_itself_as_a_dialog()
    {
        var cut = OpenDismissDialog(out _);

        var dialog = cut.Find(".modal");
        Assert.Equal("dialog", dialog.GetAttribute("role"));
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));

        // O rótulo tem de apontar para um id que EXISTE: aria-labelledby
        // pendurado no vazio faz o leitor anunciar um diálogo sem nome.
        var labelledBy = dialog.GetAttribute("aria-labelledby");
        Assert.False(string.IsNullOrWhiteSpace(labelledBy));
        Assert.NotNull(cut.Find($"#{labelledBy}"));
    }

    [Fact]
    public void Escape_should_close_the_dismiss_dialog()
    {
        var cut = OpenDismissDialog(out _);

        cut.Find(".modal").KeyDown(key: "Escape");

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".modal")));
    }

    [Fact]
    public void Another_key_should_not_close_the_dialog()
    {
        var cut = OpenDismissDialog(out _);

        // Guard contra fechar em qualquer tecla: digitar o motivo produz
        // keydown a cada letra, e um diálogo que some ao teclar é inutilizável.
        cut.Find(".modal").KeyDown(key: "a");

        Assert.NotEmpty(cut.FindAll(".modal"));
    }

    [Fact]
    public void Escape_should_close_the_create_link_dialog()
    {
        var cut = OpenCreateLinkDialog();

        cut.Find(".modal").KeyDown(key: "Escape");

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".modal")));
    }

    [Fact]
    public void The_create_link_dialog_should_announce_itself_as_a_dialog()
    {
        var cut = OpenCreateLinkDialog();

        var dialog = cut.Find(".modal");
        Assert.Equal("dialog", dialog.GetAttribute("role"));
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.NotNull(cut.Find($"#{dialog.GetAttribute("aria-labelledby")}"));
    }

    private IRenderedComponent<NpsPage> OpenDismissDialog(out ProjectsTestHelpers.MultiStubHttpMessageHandler handler)
    {
        var cut = Render(out handler);

        cut.Find(".kcard__kebab").Click();
        cut.Find(".kcard__menu-item").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".modal")));
        return cut;
    }

    private IRenderedComponent<NpsPage> OpenCreateLinkDialog()
    {
        var cut = Render(out _);

        // O do cabeçalho da página, não o do card: os dois se chamam "Gerar link".
        cut.Find("button.brq-button--primary").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".modal")));
        return cut;
    }

    private IRenderedComponent<NpsPage> Render(out ProjectsTestHelpers.MultiStubHttpMessageHandler handler)
    {
        handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        for (var i = 0; i < 6; i++)
        {
            handler.AddResponse(HttpMethod.Get, DashboardJson, HttpStatusCode.OK);
            handler.AddResponse(HttpMethod.Get, ProjectsJson, HttpStatusCode.OK);
        }

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<NpsClient>();

        var cut = RenderComponent<NpsPage>(p => p.Add(x => x.Tab, "coleta"));
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
