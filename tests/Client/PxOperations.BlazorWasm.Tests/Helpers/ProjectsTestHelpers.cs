using System.Net;
using System.Net.Http;
using System.Text;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Tests.Helpers;

internal static class ProjectsTestHelpers
{
    // ── HTTP CLIENT FACTORY ──────────────────────────────────────────────────

    internal static HttpClient CreateClient(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpClient(new StubHttpMessageHandler(content, statusCode))
        {
            BaseAddress = new Uri("http://localhost")
        };
    }

    internal static HttpClient CreateEmptyClient()
        => CreateClient("[]");

    internal static HttpClient CreateDelayedClient()
        => new HttpClient(new DelayedHttpMessageHandler()) { BaseAddress = new Uri("http://localhost") };

    // ── STUB HANDLERS ────────────────────────────────────────────────────────

    internal sealed class StubHttpMessageHandler(string content, HttpStatusCode statusCode) : HttpMessageHandler
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

    internal sealed class MultiStubHttpMessageHandler : HttpMessageHandler
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

    internal sealed class DelayedHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    // ── PROJECT DATA FACTORIES ───────────────────────────────────────────────

    internal static ProjectResponse MakeProject(
        int id = 1,
        string dc = "DC1",
        string status = "Em andamento",
        string name = "Projeto Teste",
        string client = "Cliente A",
        string type = "Squad",
        string? startDate = "2026-01-01",
        string? endDate = "2026-12-31",
        string? deliveryManager = null,
        string renewal = "None",
        string? renewalObservation = null) => new ProjectResponse
        {
            Id = id,
            Dc = dc,
            Status = status,
            Name = name,
            Client = client,
            Type = type,
            StartDate = startDate,
            EndDate = endDate,
            DeliveryManager = deliveryManager,
            Renewal = renewal,
            RenewalObservation = renewalObservation
        };

    /// <summary>Serializa um array de projetos como JSON (GET /projects).</summary>
    internal static string ProjectsJson(params ProjectResponse[] projects)
    {
        static string Str(string? v) => v is null ? "null" : $"\"{v}\"";
        var items = projects.Select(p =>
            $"{{\"id\":{p.Id},\"dc\":{Str(p.Dc)},\"status\":{Str(p.Status)},\"name\":{Str(p.Name)},\"client\":{Str(p.Client)},\"type\":{Str(p.Type)},\"startDate\":{Str(p.StartDate)},\"endDate\":{Str(p.EndDate)},\"deliveryManager\":{Str(p.DeliveryManager)},\"renewal\":{Str(p.Renewal)},\"renewalObservation\":{Str(p.RenewalObservation)}}}");
        return $"[{string.Join(",", items)}]";
    }

    /// <summary>Serializa um único projeto como JSON (POST /projects, PATCH /projects/{id}).</summary>
    internal static string ProjectJson(ProjectResponse p)
    {
        static string Str(string? v) => v is null ? "null" : $"\"{v}\"";
        return $"{{\"id\":{p.Id},\"dc\":{Str(p.Dc)},\"status\":{Str(p.Status)},\"name\":{Str(p.Name)},\"client\":{Str(p.Client)},\"type\":{Str(p.Type)},\"startDate\":{Str(p.StartDate)},\"endDate\":{Str(p.EndDate)},\"deliveryManager\":{Str(p.DeliveryManager)},\"renewal\":{Str(p.Renewal)},\"renewalObservation\":{Str(p.RenewalObservation)}}}";
    }
}
