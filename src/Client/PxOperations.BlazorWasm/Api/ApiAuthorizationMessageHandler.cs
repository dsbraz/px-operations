using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace PxOperations.BlazorWasm.Api;

/// <summary>
/// Anexa o token da pessoa logada a toda chamada ao API. Quando o token não
/// renova sozinho (sessão expirada), leva de volta ao login em vez de deixar
/// cada tela mostrar um erro genérico de carregamento.
/// </summary>
public sealed class ApiAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public ApiAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation,
        Uri apiBaseAddress,
        string scope)
        : base(provider, navigation)
    {
        ConfigureHandler(authorizedUrls: [apiBaseAddress.AbsoluteUri], scopes: [scope]);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (AccessTokenNotAvailableException exception)
        {
            exception.Redirect();
            throw;
        }
    }
}
