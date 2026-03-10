using System.Net;
using System.Net.Http;
using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Projects;

namespace PxOperations.BlazorWasm.Tests;

public sealed class ProjectsPageTests : TestContext
{
    // ── LIST ────────────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_render_table_with_projects()
    {
        Services.AddScoped(_ => CreateClient("""
            [{"id":1,"dc":"DC1","status":"Em andamento","name":"Alpha","client":"Client A","type":"Squad","startDate":"2026-01-01","endDate":"2026-12-31","deliveryManager":"John","renewal":"None","renewalObservation":null}]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Alpha", component.Markup);
            Assert.Contains("DC1", component.Markup);
            Assert.Contains("Client A", component.Markup);
        });
    }

    [Fact]
    public void Page_should_render_empty_state()
    {
        Services.AddScoped(_ => CreateClient("[]", HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Nenhum projeto encontrado", component.Markup);
        });
    }

    [Fact]
    public void Page_should_show_loading()
    {
        Services.AddScoped(_ => new HttpClient(new DelayedHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost")
        });
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        Assert.Contains("Carregando", component.Markup);
    }

    // ── STATS BAR ───────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_render_stats_bar_with_project_count()
    {
        Services.AddScoped(_ => CreateClient("""
            [
              {"id":1,"dc":"DC1","status":"Em andamento","name":"Alpha","client":"CPFL","type":"Squad","startDate":"2026-01-01","endDate":"2026-12-31","deliveryManager":null,"renewal":"None","renewalObservation":null},
              {"id":2,"dc":"DC2","status":"Encerrado","name":"Beta","client":"CPFL","type":"Squad","startDate":"2025-01-01","endDate":"2025-06-30","deliveryManager":null,"renewal":"Aprovada","renewalObservation":null}
            ]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Projetos", component.Markup);
            Assert.Contains("Em andamento", component.Markup);
        });
    }

    // ── MODAL ───────────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_show_create_modal_when_button_clicked()
    {
        Services.AddScoped(_ => CreateClient("[]", HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() => Assert.DoesNotContain("overlay open", component.Markup));

        component.Find("button.btn-purple").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Novo Projeto", component.Markup);
            Assert.Contains("overlay open", component.Markup);
        });
    }

    [Fact]
    public void Page_should_open_edit_modal_with_project_data()
    {
        Services.AddScoped(_ => CreateClient("""
            [{"id":1,"dc":"DC2","status":"Programado","name":"Portal X","client":"Acme","type":"Escopo Fechado","startDate":"2026-03-01","endDate":"2026-09-01","deliveryManager":"Flavia de Castro","renewal":"Pendente","renewalObservation":"Obs test"}]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() => Assert.Contains("Portal X", component.Markup));

        component.Find("button.ibtn:not(.del)").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Editar Projeto", component.Markup);
            Assert.Contains("overlay open", component.Markup);
            Assert.Contains("Portal X", component.Markup);
        });
    }

    [Fact]
    public void Page_should_close_modal_on_cancel()
    {
        Services.AddScoped(_ => CreateClient("[]", HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.Find("button.btn-purple").Click();
        component.WaitForAssertion(() => Assert.Contains("overlay open", component.Markup));

        // click the Cancel button inside the modal footer
        component.Find(".mfoot .btn-ghost").Click();

        component.WaitForAssertion(() => Assert.DoesNotContain("overlay open", component.Markup));
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_delete_project_and_remove_from_table()
    {
        var handler = new MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, """
            [{"id":1,"dc":"DC1","status":"Em andamento","name":"ToDelete","client":"C","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null}]
            """, HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Delete, "", HttpStatusCode.NoContent);

        Services.AddScoped(_ => new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() => Assert.Contains("ToDelete", component.Markup));

        component.Find("button.ibtn.del").Click();

        component.WaitForAssertion(() => Assert.DoesNotContain("ToDelete", component.Markup));
    }

    // ── FILTERING ───────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_filter_projects_by_search_term()
    {
        Services.AddScoped(_ => CreateClient("""
            [
              {"id":1,"dc":"DC1","status":"Em andamento","name":"Alpha Project","client":"CPFL","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null},
              {"id":2,"dc":"DC2","status":"Em andamento","name":"Beta Project","client":"Alelo","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null}
            ]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Alpha Project", component.Markup);
            Assert.Contains("Beta Project", component.Markup);
        });

        component.Find("input[type=text]").Input("Alpha");

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Alpha Project", component.Markup);
            Assert.DoesNotContain("Beta Project", component.Markup);
        });
    }

    // ── HELPERS ─────────────────────────────────────────────────────────────

    private static HttpClient CreateClient(string content, HttpStatusCode statusCode)
    {
        return new HttpClient(new StubHttpMessageHandler(content, statusCode))
        {
            BaseAddress = new Uri("http://localhost")
        };
    }

    private sealed class StubHttpMessageHandler(string content, HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private sealed class MultiStubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpMethod Method, string Content, HttpStatusCode Status)> _responses = new();

        public void AddResponse(HttpMethod method, string content, HttpStatusCode status)
            => _responses.Enqueue((method, content, status));

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_responses.TryDequeue(out var r))
            {
                var response = new HttpResponseMessage(r.Status)
                {
                    Content = new StringContent(r.Content, Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class DelayedHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
