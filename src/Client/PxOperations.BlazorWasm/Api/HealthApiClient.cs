using System.Net.Http.Json;

namespace PxOperations.BlazorWasm.Api;

public sealed class HealthApiClient(HttpClient httpClient)
{
    public async Task<HealthStatusResponse?> GetReadinessAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<HealthStatusResponse>("health/ready", cancellationToken);
    }
}
