using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.Api.IntegrationTests.Infrastructure;

namespace PxOperations.Api.IntegrationTests.Authentication;

[Collection(ApiIntegrationCollection.Name)]
public sealed class AuthenticationEndpointsTests(PostgreSqlFixture fixture)
{
    private const string ProtectedPath = "/api/team-members";

    [Fact]
    public async Task Request_without_token_should_return_unauthorized()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = AnonymousClient(factory);

        var response = await client.GetAsync(ProtectedPath);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Brq_member_token_should_be_accepted()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = ClientWith(factory, TestTokens.Member());

        var response = await client.GetAsync(ProtectedPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Guest_account_in_the_brq_tenant_should_be_forbidden_with_an_explanation()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = ClientWith(factory, TestTokens.Create(account: TestTokens.GuestAccount));

        var response = await client.GetAsync(ProtectedPath);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("BRQ", body);
    }

    [Fact]
    public async Task Token_without_account_type_should_be_forbidden()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = ClientWith(factory, TestTokens.Create(account: null));

        var response = await client.GetAsync(ProtectedPath);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Token_issued_by_another_tenant_should_return_unauthorized()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = ClientWith(factory, TestTokens.Create(tenantId: "33333333-3333-3333-3333-333333333333"));

        var response = await client.GetAsync(ProtectedPath);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_for_another_audience_should_return_unauthorized()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = ClientWith(factory, TestTokens.Create(audience: "44444444-4444-4444-4444-444444444444"));

        var response = await client.GetAsync(ProtectedPath);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_endpoints_should_stay_anonymous(string path)
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = AnonymousClient(factory);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Public_nps_survey_should_stay_anonymous()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var client = AnonymousClient(factory);

        var response = await client.GetAsync($"/api/nps/public/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Startup_should_fail_when_the_tenant_is_not_configured()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString, tenantId: "");

        Assert.ThrowsAny<Exception>(() => factory.CreateClient());
    }

    /// <summary>
    /// A política padrão exige login em tudo; a exceção é marcar
    /// [AllowAnonymous]. Esta lista é a fronteira: um anônimo novo precisa
    /// entrar aqui de propósito.
    /// </summary>
    [Fact]
    public async Task Only_health_public_nps_and_openapi_should_allow_anonymous_access()
    {
        await using var factory = new ApiWebApplicationFactory(fixture.ConnectionString);
        using var _ = factory.CreateClient();

        var anonymous = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .SelectMany(endpoint => (endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? ["*"])
                .Select(method => $"{method} {endpoint.RoutePattern.RawText?.TrimStart('/')}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "GET api/nps/public/{token:guid}",
                "GET health/live",
                "GET health/ready",
                "GET openapi/{documentName}.json",
                "POST api/nps/public/{token:guid}/responses"
            ],
            anonymous);
    }

    private static HttpClient AnonymousClient(ApiWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        return client;
    }

    private static HttpClient ClientWith(ApiWebApplicationFactory factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
