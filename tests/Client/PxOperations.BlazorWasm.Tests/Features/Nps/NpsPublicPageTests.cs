using System.Net;
using System.Text.Json;
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

    [Fact]
    public void Public_form_css_should_use_theme_aware_foundation_tokens_for_every_visual_layer()
    {
        var css = PublicFormCss();

        Assert.Contains("background: var(--color-bg)", css);
        Assert.Contains("background: var(--color-surface-2)", css);
        Assert.Contains("background: var(--color-surface)", css);
        Assert.Contains("border: 1px solid var(--color-border)", css);
        Assert.Contains("color: var(--color-text)", css);

        Assert.DoesNotContain("var(--bg)", css);
        Assert.DoesNotContain("var(--surface)", css);
        Assert.DoesNotContain("var(--text)", css);
        Assert.DoesNotContain("var(--border)", css);
        Assert.DoesNotContain("var(--purple)", css);
        Assert.DoesNotContain("var(--muted)", css);
        Assert.DoesNotContain("var(--red)", css);
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

    private static string PublicFormCss()
    {
        var manifestPath = Path.Combine(
            AppContext.BaseDirectory,
            "PxOperations.BlazorWasm.staticwebassets.runtime.json");
        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = manifest.RootElement;
        var asset = root
            .GetProperty("Root")
            .GetProperty("Children")
            .GetProperty("PxOperations.BlazorWasm.styles.css")
            .GetProperty("Asset");
        var contentRoot = root
            .GetProperty("ContentRoots")[asset.GetProperty("ContentRootIndex").GetInt32()]
            .GetString()!;
        var bundle = File.ReadAllText(Path.Combine(contentRoot, asset.GetProperty("SubPath").GetString()!));
        const string marker = "/* /Features/Nps/NpsPublicPage.razor.rz.scp.css */";
        var start = bundle.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, "Seção CSS do formulário público não encontrada.");
        var end = bundle.IndexOf("/* /", start + marker.Length, StringComparison.Ordinal);
        return end < 0 ? bundle[start..] : bundle[start..end];
    }
}
