using System.Net;
using System.Net.Http;
using System.Text;

namespace PxOperations.BlazorWasm.Tests.Helpers;

/// <summary>
/// Responde as leituras na ordem em que foram enfileiradas e segura as
/// requisições do método escolhido até o teste liberar. Serve para agir na
/// tela enquanto uma gravação ainda está em voo.
/// </summary>
internal sealed class GatedHttpMessageHandler(HttpMethod gatedMethod) : HttpMessageHandler
{
    private readonly Queue<string> responses = new();
    private readonly TaskCompletionSource<HttpResponseMessage> gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void AddResponse(string json) => responses.Enqueue(json);

    public void Release(HttpStatusCode status)
        => gate.SetResult(new HttpResponseMessage(status)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method == gatedMethod)
            return gate.Task;

        var json = responses.TryDequeue(out var next) ? next : "[]";
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
    }
}
