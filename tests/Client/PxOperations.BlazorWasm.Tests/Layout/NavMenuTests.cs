using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Layout;

namespace PxOperations.BlazorWasm.Tests.Layout;

public sealed class NavMenuTests : BunitContext
{
    private readonly BunitAuthorizationContext authorization;

    public NavMenuTests()
    {
        authorization = AddAuthorization();
    }

    [Fact]
    public void NavMenu_should_show_who_is_signed_in()
    {
        authorization.SetAuthorized("Pessoa BRQ");

        var cut = Render<NavMenu>();

        Assert.Equal("Pessoa BRQ", cut.Find(".topbar-user__name").TextContent.Trim());
    }

    [Fact]
    public void Sign_out_should_go_through_the_microsoft_logout()
    {
        authorization.SetAuthorized("Pessoa BRQ");
        var navigation = Services.GetRequiredService<NavigationManager>();

        var cut = Render<NavMenu>();
        cut.Find("button.topbar-user__logout").Click();

        Assert.EndsWith("/authentication/logout", navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void NavMenu_should_render_milestones_entry()
    {
        var cut = Render<NavMenu>();

        Assert.Contains("Marcos", cut.Markup);
        Assert.Contains("/milestones", cut.Markup);
        Assert.Contains("NPS", cut.Markup);
        Assert.Contains("/nps", cut.Markup);
    }

    /// <summary>
    /// Os outros módulos acendem no menu; Projetos ficava apagado em
    /// /projects porque o link só casava com "/" exato.
    /// </summary>
    [Fact]
    public void NavMenu_should_highlight_projects_on_the_projects_route()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/projects");

        var cut = Render<NavMenu>();

        var projects = cut.FindAll("nav a").Single(link => link.TextContent.Trim() == "Projetos");
        Assert.Contains("active", projects.ClassList);
    }

    [Fact]
    public void NavMenu_should_hide_module_links_on_public_nps_form()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo($"/nps/{Guid.NewGuid()}");
        authorization.SetAuthorized("Pessoa BRQ");

        var cut = Render<NavMenu>();

        Assert.Contains("Operations PX", cut.Markup);
        Assert.Contains("brq-logo", cut.Markup);
        Assert.DoesNotContain("Projetos", cut.Markup);
        Assert.DoesNotContain("/milestones", cut.Markup);
        Assert.DoesNotContain("/project-health", cut.Markup);
        Assert.DoesNotContain(">NPS<", cut.Markup);
        Assert.DoesNotContain("Sair", cut.Markup);
    }
}
