using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps;
using PxOperations.BlazorWasm.Tests.Helpers;
using PxOperations.Ui;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

public sealed class NpsPublicPageTests : TestContext
{
    public NpsPublicPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddPxOperationsUi();
    }

    [Fact]
    public void Complete_portuguese_form_should_render_server_scales_in_the_required_order()
    {
        var token = Guid.NewGuid();
        var handler = RegisterClient(PublicJson(token, "complete", "pt", "open"));
        handler.AddResponse(HttpMethod.Post, ResponseJson(), HttpStatusCode.Created);

        var cut = RenderComponent<NpsPublicPage>(parameters => parameters.Add(page => page.Token, token));

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(10, cut.FindAll(".nps-score-scale button").Count);
            Assert.Equal(4, cut.FindAll(".nps-aspect").Count);
            var markup = cut.Markup;
            Assert.True(markup.IndexOf("Comentário", StringComparison.Ordinal) < markup.IndexOf("Qualidade", StringComparison.Ordinal));
            Assert.True(markup.IndexOf("Valor para o negócio", StringComparison.Ordinal) < markup.IndexOf("Nome (opcional)", StringComparison.Ordinal));
            Assert.Contains("Privacidade", markup);
        });

        cut.Find(".nps-score-scale button").Click();
        cut.Find(".nps-submit").Click();
        cut.WaitForAssertion(() => Assert.Contains("Sua resposta foi registrada.", cut.Markup));
    }

    [Theory]
    [InlineData("en", "How likely are you to recommend BRQ?", "Privacy", "Submit response")]
    [InlineData("es", "¿Qué probabilidad hay de que recomiendes BRQ?", "Privacidad", "Enviar respuesta")]
    public void Simplified_form_should_localize_all_text_and_hide_aspects(
        string language,
        string question,
        string privacy,
        string submit)
    {
        var token = Guid.NewGuid();
        RegisterClient(PublicJson(token, "simplified", language, "open"));

        var cut = RenderComponent<NpsPublicPage>(parameters => parameters.Add(page => page.Token, token));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(question, cut.Markup);
            Assert.Contains(privacy, cut.Markup);
            Assert.Contains(submit, cut.Markup);
            Assert.Empty(cut.FindAll(".nps-aspect"));
        });
    }

    [Theory]
    [InlineData("expired", "expirou")]
    [InlineData("closed", "indisponível")]
    [InlineData("waived", "indisponível")]
    [InlineData("already_answered", "já foi respondido")]
    public void Availability_should_render_distinct_final_states(string availability, string expected)
    {
        var token = Guid.NewGuid();
        RegisterClient(PublicJson(token, "simplified", "pt", availability));

        var cut = RenderComponent<NpsPublicPage>(parameters => parameters.Add(page => page.Token, token));

        cut.WaitForAssertion(() => Assert.Contains(expected, cut.Markup, StringComparison.OrdinalIgnoreCase));
        Assert.Empty(cut.FindAll(".nps-submit"));
    }

    [Fact]
    public void Invalid_token_should_have_its_own_state()
    {
        var token = Guid.NewGuid();
        RegisterClient("{}", HttpStatusCode.NotFound);

        var cut = RenderComponent<NpsPublicPage>(parameters => parameters.Add(page => page.Token, token));

        cut.WaitForAssertion(() => Assert.Contains("Link inválido", cut.Markup));
    }

    private ProjectsTestHelpers.MultiStubHttpMessageHandler RegisterClient(
        string response,
        HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, response, status);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        Services.AddScoped(_ => httpClient);
        Services.AddScoped<NpsClient>();
        return handler;
    }

    private static string PublicJson(Guid token, string format, string language, string availability) => $$$"""
        {
          "token":"{{{token}}}","projectId":1,"projectName":"Projeto Público","client":"Cliente", "dispatchId":2,
          "format":"{{{format}}}","language":"{{{language}}}","expiresAt":"2026-08-21T12:00:00Z","availability":"{{{availability}}}","isGeneric":true,
          "scoreScale":{"minimum":1,"maximum":10},
          "aspects":[
            {"code":"quality","label":"Qualidade","scale":{"minimum":1,"maximum":5}},
            {"code":"schedule","label":"Prazo","scale":{"minimum":1,"maximum":5}},
            {"code":"communication","label":"Comunicação","scale":{"minimum":1,"maximum":5}},
            {"code":"businessValue","label":"Valor para o negócio","scale":{"minimum":1,"maximum":5}}
          ]
        }
        """;

    private static string ResponseJson() => """
        {"id":1,"projectId":1,"projectName":"Projeto Público","dispatchId":2,"targetId":3,"contactId":null,"contactName":null,"contactEmail":null,"format":"complete","formatLabel":"Completo","score":10,"classification":"promoter","classificationLabel":"Promotor","quality":null,"schedule":null,"communication":null,"businessValue":null,"comment":null,"respondentName":null,"respondentEmail":null,"submittedAt":"2026-08-01T12:00:00Z"}
        """;
}
