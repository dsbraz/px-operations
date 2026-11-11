using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Tests.Api;

public sealed class ApiAuthorizationMessageHandlerTests : BunitContext
{
    private static readonly Uri ApiBaseAddress = new("http://localhost/");

    /// <summary>
    /// O token silencioso para de renovar (sessão expirada, refresh token
    /// vencido). Sem o redirect, cada tela engolia a exceção como "erro ao
    /// carregar" e nem recarregar resolvia, porque a conta segue em cache.
    /// </summary>
    [Fact]
    public async Task Missing_token_should_send_the_person_back_to_the_login()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/projects");
        using var client = new HttpClient(new ApiAuthorizationMessageHandler(
            new TokenRequiringLogin(),
            navigation,
            ApiBaseAddress,
            "api://px/access_as_user")
        {
            InnerHandler = new OkHandler()
        });

        await Assert.ThrowsAsync<AccessTokenNotAvailableException>(
            () => client.GetAsync(new Uri(ApiBaseAddress, "api/projects")));

        Assert.EndsWith("/authentication/login", navigation.Uri, StringComparison.Ordinal);
    }

    private sealed class TokenRequiringLogin : IAccessTokenProvider
    {
        public ValueTask<AccessTokenResult> RequestAccessToken() => RequestAccessToken(new AccessTokenRequestOptions());

        public ValueTask<AccessTokenResult> RequestAccessToken(AccessTokenRequestOptions options)
            => ValueTask.FromResult(new AccessTokenResult(
                AccessTokenResultStatus.RequiresRedirect,
                null!,
                "authentication/login",
                new InteractiveRequestOptions { Interaction = InteractionType.GetToken, ReturnUrl = "http://localhost/projects" }));
    }

    private sealed class OkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
