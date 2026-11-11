using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Features.Authentication;

namespace PxOperations.BlazorWasm.Tests.Features.Authentication;

public sealed class RedirectToLoginTests : BunitContext
{
    [Fact]
    public void Should_send_the_visitor_to_the_microsoft_login()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/projects");

        Render<RedirectToLogin>();

        Assert.EndsWith("/authentication/login", navigation.Uri, StringComparison.Ordinal);
    }
}
