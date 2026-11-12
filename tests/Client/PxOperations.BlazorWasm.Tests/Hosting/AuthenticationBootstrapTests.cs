namespace PxOperations.BlazorWasm.Tests.Hosting;

/// <summary>
/// O login M365 depende do AuthenticationService.js do pacote MSAL, carregado
/// por uma tag solta no index.html — nenhum código C# quebra se ela sumir.
/// </summary>
public sealed class AuthenticationBootstrapTests
{
    private const string AuthenticationServicePath =
        "_content/Microsoft.Authentication.WebAssembly.Msal/AuthenticationService.js";

    [Fact]
    public void Index_should_load_the_msal_authentication_service_before_blazor()
    {
        var html = File.ReadAllText(ThemeBootstrapTests.ResolveAsset("index.html"));

        var authenticationIndex = html.IndexOf(AuthenticationServicePath, StringComparison.Ordinal);
        var blazorIndex = html.IndexOf("_framework/blazor.webassembly", StringComparison.Ordinal);
        Assert.True(authenticationIndex >= 0, "index.html precisa carregar AuthenticationService.js: sem ele o login não abre.");
        Assert.True(authenticationIndex < blazorIndex, "AuthenticationService.js precisa vir antes do blazor.webassembly.js.");
    }
}
