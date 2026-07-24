using Bunit;
using PxOperations.Ui.Components.DataDisplay;

namespace PxOperations.Ui.Tests.Components;

public sealed class BrqTableShellTests : BunitContext
{
    [Fact]
    public void Table_shell_should_render_caption_and_named_scroll_region()
    {
        var cut = Render<BrqTableShell>(parameters => parameters
            .Add(component => component.Caption, "Carteira de projetos")
            .AddChildContent("""
                <thead><tr><th scope="col">Projeto</th></tr></thead>
                <tbody><tr><td>Alpha</td></tr></tbody>
                """));

        Assert.Equal("Carteira de projetos", cut.Find("caption").TextContent);

        var region = cut.Find("[role='region'].table-wrap");
        Assert.Equal("0", region.GetAttribute("tabindex"));
        Assert.Equal("Carteira de projetos", region.GetAttribute("aria-label"));
        Assert.NotNull(cut.Find("table.data-table"));
        Assert.NotNull(cut.Find("th[scope='col']"));
    }
}
