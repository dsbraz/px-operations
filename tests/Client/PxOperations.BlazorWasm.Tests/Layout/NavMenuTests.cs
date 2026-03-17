using Bunit;
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
    }
}
