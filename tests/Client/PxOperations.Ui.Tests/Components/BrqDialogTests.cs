using Bunit;
using PxOperations.Ui.Components.Overlays;

namespace PxOperations.Ui.Tests.Components;

public sealed class BrqDialogTests : BunitContext
{
    [Fact]
    public void Dialog_should_use_native_element_and_accessible_title()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = Render<BrqDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Detalhes do projeto")
            .AddChildContent("<p>Projeto Alpha</p>"));

        var dialog = cut.Find("dialog");
        var title = cut.Find("h2");

        Assert.Equal(title.Id, dialog.GetAttribute("aria-labelledby"));
        Assert.Equal("true", dialog.GetAttribute("data-open"));
        Assert.Contains("Projeto Alpha", dialog.TextContent);
        Assert.NotNull(cut.Find("button[aria-label='Fechar diálogo']"));
    }

    [Fact]
    public void Close_button_should_notify_the_owner()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        bool? open = true;
        var closed = false;

        var cut = Render<BrqDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Detalhes")
            .Add(component => component.OpenChanged,
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create<bool>(
                    this, value => open = value))
            .Add(component => component.OnClosed,
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create(
                    this, () => closed = true)));

        cut.Find("button[aria-label='Fechar diálogo']").Click();

        Assert.False(open);
        Assert.True(closed);
    }
}
