using Bunit;
using PxOperations.Ui.Components.Forms;

namespace PxOperations.Ui.Tests.Components;

public sealed class BrqFilterPanelTests : BunitContext
{
    [Fact]
    public async Task Filter_panel_should_close_when_focus_leaves_the_popover()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<BrqFilterPanel>(parameters => parameters
            .AddChildContent("<label>DC</label>"));

        cut.Find("button.fmenu__btn").Click();
        Assert.NotNull(cut.Find("section.fmenu__pop"));

        await cut.InvokeAsync(() => cut.Instance.CloseFromOutsideAsync());

        Assert.Empty(cut.FindAll("section.fmenu__pop"));
    }
}
