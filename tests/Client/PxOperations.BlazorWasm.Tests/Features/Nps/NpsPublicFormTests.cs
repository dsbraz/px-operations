using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

/// <summary>
/// F4: link compartilhado multi-resposta, prazo visível ao respondente e
/// questionário substituído pelo aviso quando o prazo termina.
/// </summary>
public sealed class NpsPublicFormTests : TestContext
{
    private static readonly Guid Token = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>
    /// D10: a nota não nasce escolhida. Um 10 pré-marcado transformaria "não
    /// respondi" em "sou promotor" e inflaria o NPS a cada envio distraído.
    /// </summary>
    [Fact]
    public void Score_should_start_unselected()
    {
        var cut = Render(SurveyJson());

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".scale__opt")));
        Assert.Empty(cut.FindAll(".scale__opt.is-selected"));
    }

    [Fact]
    public void Submitting_without_a_score_should_warn_instead_of_posting()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, SurveyJson(), HttpStatusCode.OK);

        var cut = Render(handler);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".scale__opt")));

        cut.Find(".next button").Click();

        cut.WaitForAssertion(() => Assert.Contains("Escolha uma nota", cut.Find(".q__error").TextContent));
        Assert.Single(handler.RequestUris);
    }

    /// <summary>D10: a escala da nota é 1 a 10 e a dos aspectos, 1 a 5.</summary>
    [Fact]
    public void Complete_form_should_use_one_to_ten_and_one_to_five()
    {
        var cut = Render(SurveyJson(format: "Completo"));

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".dim")));

        var nota = cut.Find(".q .scale:not(.scale--sm)").QuerySelectorAll(".scale__opt");
        Assert.Equal(10, nota.Length);
        Assert.Equal("1", nota[0].TextContent.Trim());
        Assert.Equal("10", nota[9].TextContent.Trim());

        var aspecto = cut.FindAll(".scale--sm")[0].QuerySelectorAll(".scale__opt");
        Assert.Equal(5, aspecto.Length);

        // B13: o quarto aspecto trocou de assunto.
        Assert.Contains("Valor gerado para o negócio", cut.Markup);
        Assert.DoesNotContain("Escopo", cut.Markup);
    }

    /// <summary>D7: o respondente vê até quando a pesquisa fica aberta.</summary>
    [Fact]
    public void Form_should_show_the_deadline()
    {
        var cut = Render(SurveyJson());

        cut.WaitForAssertion(() => Assert.Contains("Aberta até", cut.Find(".intro__deadline").TextContent));
    }

    /// <summary>
    /// D7: passado o prazo o questionário dá lugar ao aviso. Deixar responder
    /// para rejeitar no envio faria o respondente preencher tudo à toa.
    /// </summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Closed_or_expired_should_replace_the_form(bool expired, bool closed)
    {
        var cut = Render(SurveyJson(isExpired: expired, isClosed: closed));

        cut.WaitForAssertion(() =>
            Assert.Contains("prazo desta pesquisa terminou", cut.Find(".heading-xl").TextContent));
        Assert.Empty(cut.FindAll(".scale__opt"));
        Assert.Empty(cut.FindAll(".next button"));
    }

    /// <summary>
    /// D1: o recibo diz que o mesmo link continua valendo — é o que faz o
    /// respondente entender que pode repassá-lo ao resto do time.
    /// </summary>
    [Fact]
    public void Receipt_should_say_the_link_is_still_valid_for_others()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, SurveyJson(), HttpStatusCode.OK);
        // O endpoint devolve a resposta criada; o cliente NSwag desserializa o
        // corpo, então um {} vazio falharia por motivo alheio ao teste.
        handler.AddResponse(HttpMethod.Post, CreatedResponseJson(), HttpStatusCode.Created);

        var cut = Render(handler);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".scale__opt")));

        cut.FindAll(".scale__opt").First(o => o.TextContent.Trim() == "9").Click();
        cut.Find(".next button").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("as outras pessoas do seu time ainda podem responder", cut.Markup));
    }

    private IRenderedComponent<NpsPublicPage> Render(string surveyJson)
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, surveyJson, HttpStatusCode.OK);
        return Render(handler);
    }

    private IRenderedComponent<NpsPublicPage> Render(ProjectsTestHelpers.MultiStubHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<NpsClient>();

        return RenderComponent<NpsPublicPage>(p => p.Add(page => page.Token, Token));
    }

    private static string CreatedResponseJson() => """
    {"id":30,"projectId":1,"projectName":"Projeto 1","dispatchId":2,"targetId":3,"contactId":null,
     "contactName":null,"contactEmail":null,"score":9,"classification":"Promotor","businessValue":null,
     "schedule":null,"quality":null,"communication":null,"tags":null,"comment":null,
     "respondentName":null,"respondentEmail":null,"submittedAt":"2026-08-26T00:00:00Z"}
    """;

    private static string SurveyJson(
        string format = "Simplificado", bool isExpired = false, bool isClosed = false)
        => $$"""
        {
          "token": "11111111-1111-1111-1111-111111111111",
          "projectId": 1, "projectName": "Projeto 1", "dispatchId": 2,
          "periodStart": "2026-08-01", "periodEnd": "2026-08-31",
          "format": "{{format}}", "language": "Português",
          "expiresAt": "2026-09-15T00:00:00Z",
          "isExpired": {{(isExpired ? "true" : "false")}},
          "isClosed": {{(isClosed ? "true" : "false")}},
          "alreadyAnswered": false
        }
        """;
}
