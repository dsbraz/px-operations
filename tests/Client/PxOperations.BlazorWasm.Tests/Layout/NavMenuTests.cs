using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Layout;

namespace PxOperations.BlazorWasm.Tests.Layout;

public sealed class NavMenuTests : TestContext
{
    [Fact]
    public void NavMenu_should_render_milestones_entry()
    {
        var cut = RenderComponent<NavMenu>();

        Assert.Contains("Marcos", cut.Markup);
        Assert.Contains("/milestones", cut.Markup);
        Assert.Contains("NPS", cut.Markup);
        Assert.Contains("/nps", cut.Markup);
    }

    [Fact]
    public void NavMenu_should_hide_module_links_on_public_nps_form()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"/nps/{Guid.NewGuid()}");

        var cut = RenderComponent<NavMenu>();

        Assert.Contains("Operations PX", cut.Markup);
        Assert.Contains("brq-logo", cut.Markup);
        Assert.DoesNotContain("Projetos", cut.Markup);
        Assert.DoesNotContain("/milestones", cut.Markup);
        Assert.DoesNotContain("/project-health", cut.Markup);
        Assert.DoesNotContain(">NPS<", cut.Markup);
    }
}
