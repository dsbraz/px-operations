using System.Net;
using System.Net.Http;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.HealthChecks;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.HealthChecks;

public sealed class HealthFormPageTests : TestContext
{
    [Fact]
    public void Page_should_show_loading_while_api_is_pending()
    {
        Services.AddScoped(_ => ProjectsTestHelpers.CreateDelayedClient());
        Services.AddScoped<HealthChecksClient>();
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<HealthFormPage>();

        Assert.Contains("Project Health", cut.Markup);
    }

    [Fact]
    public void Page_should_render_form_fields()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, ProjectsTestHelpers.ProjectsJson(
            ProjectsTestHelpers.MakeProject(1, "DC1", name: "Projeto A"),
            ProjectsTestHelpers.MakeProject(2, "DC2", name: "Projeto B")
        ), System.Net.HttpStatusCode.OK);

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<HealthChecksClient>();
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<HealthFormPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Seu e-mail", cut.Markup);
            Assert.Contains("Delivery Center", cut.Markup);
            Assert.Contains("Checklist de Práticas", cut.Markup);
            Assert.Contains("Escopo", cut.Markup);
            Assert.Contains("Cronograma", cut.Markup);
            Assert.Contains("Qualidade", cut.Markup);
            Assert.Contains("Satisfação", cut.Markup);
            Assert.Contains("Destaques", cut.Markup);
        });
    }

    [Fact]
    public void Page_should_show_validation_error_when_email_is_empty()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, "[]", System.Net.HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<HealthChecksClient>();
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<HealthFormPage>();

        cut.WaitForAssertion(() =>
        {
            var submitBtn = cut.Find(".health-footer .btn-purple");
            submitBtn.Click();
            Assert.Contains("Informe seu e-mail", cut.Markup);
        });
    }

    [Fact]
    public void Page_should_toggle_practice_checkboxes()
    {
        var handler = new ProjectsTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, "[]", System.Net.HttpStatusCode.OK);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => client);
        Services.AddScoped<HealthChecksClient>();
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<HealthFormPage>();

        cut.WaitForAssertion(() =>
        {
            var items = cut.FindAll(".practice-item");
            Assert.Equal(5, items.Count);
        });

        cut.Find(".practice-item").Click();
        Assert.Contains("1 de 5", cut.Markup);

        cut.FindAll(".practice-item")[1].Click();
        Assert.Contains("2 de 5", cut.Markup);

        cut.Find(".practice-item").Click();
        Assert.Contains("1 de 5", cut.Markup);
    }
}
