using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

/// <summary>
/// B4/F4: os três freios do link aberto, pelo lado do respondente. O critério
/// de aceite é explícito quanto ao tom — "reenvio no mesmo navegador é
/// bloqueado com mensagem amigável" —, e toda mensagem tem de deixar claro que
/// o link SEGUE valendo para o resto do time (D1).
/// </summary>
public sealed class NpsAntiAbuseFormTests : TestContext
{
    private static readonly Guid Token = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string FlagKey = "nps-answered-11111111-1111-1111-1111-111111111111";

    [Fact]
    public void A_browser_that_already_answered_should_see_the_notice_instead_of_the_form()
    {
        var cut = Render(alreadyAnsweredInBrowser: true, out _);

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".scale__opt"));
            Assert.Contains("já respondeu por este navegador", cut.Markup);
            // D1: o aviso não pode soar como fim da coleta.
            Assert.Contains("as outras pessoas do seu time ainda podem responder", cut.Markup);
        });
    }

    [Fact]
    public void A_browser_that_has_not_answered_should_see_the_form()
    {
        var cut = Render(alreadyAnsweredInBrowser: false, out _);

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".scale__opt")));
    }

    [Fact]
    public void Answering_should_mark_the_browser()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, SurveyJson, HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Post, CreatedResponseJson, HttpStatusCode.Created);
        var cut = Render(handler, alreadyAnsweredInBrowser: false);

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".scale__opt")));
        cut.FindAll(".scale__opt")[8].Click();
        cut.Find(".next button").Click();

        // Sem a marca, um F5 devolveria o formulário em branco e o reenvio
        // distraído — que é justamente o que o freio existe para barrar.
        cut.WaitForAssertion(() => JSInterop.VerifyInvoke("localStorage.setItem"));
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, "Este e-mail já respondeu esta pesquisa")]
    [InlineData(HttpStatusCode.TooManyRequests, "muitas respostas deste ponto de acesso")]
    public void Each_server_brake_should_have_its_own_message(HttpStatusCode status, string expected)
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, SurveyJson, HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Post, """{"detail":"barrado"}""", status);
        var cut = Render(handler, alreadyAnsweredInBrowser: false);

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".scale__opt")));
        cut.FindAll(".scale__opt")[8].Click();
        cut.Find(".next button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(expected, cut.Find(".q__error").TextContent);
            // O formulário continua na tela: quem preencheu não pode perder tudo.
            Assert.NotEmpty(cut.FindAll(".scale__opt"));
        });
    }

    private IRenderedComponent<NpsPublicPage> Render(bool alreadyAnsweredInBrowser, out ProjectsTestHelpers.MultiStubHttpMessageHandler handler)
    {
        handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, SurveyJson, HttpStatusCode.OK);
        return Render(handler, alreadyAnsweredInBrowser);
    }

    private IRenderedComponent<NpsPublicPage> Render(ProjectsTestHelpers.MultiStubHttpMessageHandler handler, bool alreadyAnsweredInBrowser)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<string?>("localStorage.getItem", FlagKey)
            .SetResult(alreadyAnsweredInBrowser ? "1" : null);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<NpsClient>();

        return RenderComponent<NpsPublicPage>(p => p.Add(page => page.Token, Token));
    }

    private const string SurveyJson = """
    {
      "token": "11111111-1111-1111-1111-111111111111",
      "projectId": 1, "projectName": "Projeto 1", "dispatchId": 2,
      "periodStart": "2026-08-01", "periodEnd": "2026-08-31",
      "format": "Simplificado", "language": "Português",
      "expiresAt": "2126-09-15T00:00:00Z", "isExpired": false,
      "isClosed": false, "alreadyAnswered": false
    }
    """;

    private const string CreatedResponseJson = """
    {"id":30,"projectId":1,"projectName":"Projeto 1","dispatchId":2,"targetId":3,"contactId":null,
     "contactName":null,"contactEmail":null,"score":9,"classification":"Promotor","businessValue":null,
     "schedule":null,"quality":null,"communication":null,"tags":null,"comment":null,
     "respondentName":null,"respondentEmail":null,"submittedAt":"2026-08-26T00:00:00Z"}
    """;
}
