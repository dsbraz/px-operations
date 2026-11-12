using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace PxOperations.BlazorWasm.Tests;

public sealed class AppTests : BunitContext
{
    /// <summary>
    /// A pesquisa pública é aberta por clientes de fora da BRQ, sem login.
    /// Enquanto o MSAL inicializa, o AuthorizeRouteView mostraria o texto
    /// padrão do framework ("Authorizing..."), em inglês.
    /// </summary>
    [Fact]
    public void Public_survey_should_not_show_the_framework_authorizing_text()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        AddAuthorization().SetAuthorizing();
        Services.GetRequiredService<NavigationManager>().NavigateTo($"http://localhost/nps/{Guid.NewGuid()}");

        var cut = Render<App>();

        Assert.DoesNotContain("Authorizing", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}
