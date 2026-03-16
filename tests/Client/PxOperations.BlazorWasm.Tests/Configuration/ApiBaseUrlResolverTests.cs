using PxOperations.BlazorWasm.Configuration;

namespace PxOperations.BlazorWasm.Tests.Configuration;

public sealed class ApiBaseUrlResolverTests
{
    [Fact]
    public void Resolve_should_fallback_to_host_base_address_when_config_is_missing()
    {
        var result = ApiBaseUrlResolver.Resolve(null, "https://px-operations-web.example.com/");

        Assert.Equal("https://px-operations-web.example.com/", result.AbsoluteUri);
    }

    [Fact]
    public void Resolve_should_expand_relative_root_to_same_origin()
    {
        var result = ApiBaseUrlResolver.Resolve("/", "https://px-operations-web.example.com/");

        Assert.Equal("https://px-operations-web.example.com/", result.AbsoluteUri);
    }

    [Fact]
    public void Resolve_should_keep_absolute_api_base_url()
    {
        var result = ApiBaseUrlResolver.Resolve(
            "https://px-operations-api.example.com/",
            "https://px-operations-web.example.com/");

        Assert.Equal("https://px-operations-api.example.com/", result.AbsoluteUri);
    }
}
