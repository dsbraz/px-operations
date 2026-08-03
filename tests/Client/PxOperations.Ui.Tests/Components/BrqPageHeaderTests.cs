using Bunit;
using PxOperations.Ui.Components.Navigation;

namespace PxOperations.Ui.Tests.Components;

/// <summary>
/// Trava a hierarquia compacta de cabeçalho no contrato do painel de NPS,
/// que é a referência visual da migração.
/// </summary>
public sealed class BrqPageHeaderTests : BunitContext
{
    [Fact]
    public void Should_render_the_compact_nps_dashboard_hierarchy()
    {
        var cut = Render<BrqPageHeader>(parameters => parameters
            .Add(component => component.Title, "Projetos")
            .Add(component => component.Description, "Carteira ativa"));

        Assert.NotNull(cut.Find("header.page-head"));
        Assert.Equal("Projetos", cut.Find("h1.page-head__title").TextContent);
        Assert.Equal("Carteira ativa", cut.Find("p.page-head__sub").TextContent);
    }
}
