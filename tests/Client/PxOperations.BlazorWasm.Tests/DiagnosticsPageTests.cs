using System.Net;
using System.Net.Http;
using System.Text;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Diagnostics;

namespace PxOperations.BlazorWasm.Tests;

public sealed class DiagnosticsPageTests : TestContext
{
    [Fact]
    public void Page_should_render_ready_status_when_api_returns_success()
    {
        Services.AddScoped(_ => CreateClient("""
            {"status":"Ready"}
            """, HttpStatusCode.OK));
        Services.AddScoped<HealthClient>();

        var component = RenderComponent<DiagnosticsPage>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Ready", component.Markup);
            Assert.DoesNotContain("Unavailable", component.Markup);
        });
    }

    [Fact]
    public void Page_should_render_error_when_api_returns_failure()
    {
        Services.AddScoped(_ => CreateClient("""
            {"status":"Database unavailable"}
            """, HttpStatusCode.ServiceUnavailable));
        Services.AddScoped<HealthClient>();

        var component = RenderComponent<DiagnosticsPage>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Unavailable", component.Markup);
        });
    }

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
}
