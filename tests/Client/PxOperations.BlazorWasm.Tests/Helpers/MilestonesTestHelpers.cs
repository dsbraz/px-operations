using System.Net;
using System.Net.Http;
using System.Text;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Tests.Helpers;

internal static class MilestonesTestHelpers
{
    internal static HttpClient CreateClient(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpClient(new StubHttpMessageHandler(content, statusCode))
        {
            BaseAddress = new Uri("http://localhost")
        };
    }

    internal static MilestoneResponse MakeMilestone(
        int id = 1,
        int projectId = 10,
        string projectName = "Projeto A",
        string projectDc = "DC1",
        string type = "Kickoff",
        string title = "Marco A",
        string date = "2026-03-20",
        string? time = null,
        string? notes = null) => new()
        {
            Id = id,
            ProjectId = projectId,
            ProjectName = projectName,
            ProjectClient = "Cliente X",
            ProjectDc = projectDc,
            Type = type,
            Title = title,
            Date = date,
            Time = time,
            Notes = notes
        };

    internal static string MilestonesJson(params MilestoneResponse[] milestones)
    {
        static string Str(string? v) => v is null ? "null" : $"\"{v}\"";
        var items = milestones.Select(m =>
            $"{{\"id\":{m.Id},\"projectId\":{m.ProjectId},\"projectName\":{Str(m.ProjectName)},\"projectClient\":{Str(m.ProjectClient)},\"projectDc\":{Str(m.ProjectDc)},\"type\":{Str(m.Type)},\"title\":{Str(m.Title)},\"date\":{Str(m.Date)},\"time\":{Str(m.Time)},\"notes\":{Str(m.Notes)}}}");
        return $"[{string.Join(",", items)}]";
    }

    internal sealed class StubHttpMessageHandler(string content, HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
        }
    }

    internal sealed class MultiStubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode StatusCode, string Content)> responses = new();

        public void AddResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
            => responses.Enqueue((statusCode, content));

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!responses.TryDequeue(out var response))
            {
                response = (HttpStatusCode.OK, "[]");
            }

            return Task.FromResult(new HttpResponseMessage(response.StatusCode)
            {
                Content = new StringContent(response.Content, Encoding.UTF8, "application/json")
            });
        }
    }
}
