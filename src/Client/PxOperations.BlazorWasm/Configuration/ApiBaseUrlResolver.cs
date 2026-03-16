namespace PxOperations.BlazorWasm.Configuration;

public static class ApiBaseUrlResolver
{
    public static Uri Resolve(string? configuredBaseUrl, string hostBaseAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostBaseAddress);

        var hostBaseUri = new Uri(hostBaseAddress, UriKind.Absolute);

        if (string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return hostBaseUri;
        }

        if (Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var absoluteUri)
            && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
        {
            return absoluteUri;
        }

        return new Uri(hostBaseUri, configuredBaseUrl);
    }
}
