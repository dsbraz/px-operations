using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PxOperations.Api.IntegrationTests.Infrastructure;

namespace PxOperations.Api.IntegrationTests.Infrastructure;

[Collection(ApiIntegrationCollection.Name)]
public sealed class ForwardedHeadersConfigurationTests(PostgreSqlFixture fixture)
{
    /// <summary>
    /// A API roda atrás do front-end do Cloud Run, cujo endereço nunca é
    /// loopback. Confiar apenas em loopback — que já é o padrão — fazia o
    /// X-Forwarded-For ser descartado e o rate limit do link público
    /// particionar pelo endereço compartilhado da plataforma, virando um balde
    /// por link em vez de um por cliente.
    /// </summary>
    [Fact]
    public async Task Forwarded_headers_should_trust_the_platform_proxy()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        var options = factory.Services.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.Empty(options.KnownProxies);
        Assert.Empty(options.KnownIPNetworks);
        Assert.Equal(1, options.ForwardLimit);
    }
}
