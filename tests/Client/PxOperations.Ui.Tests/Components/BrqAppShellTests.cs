using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.Ui.Components.Navigation;
using PxOperations.Ui.Theming;

namespace PxOperations.Ui.Tests.Components;

public sealed class BrqAppShellTests : BunitContext
{
    public BrqAppShellTests()
    {
        Services.AddSingleton<IThemeService>(new FakeThemeService());
    }

    [Fact]
    public void Shell_should_render_landmarks_skip_link_and_supplied_navigation()
    {
        var navigation = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        navigation.NavigateTo("/preview/projects");

        var items = new[]
        {
            new BrqNavigationItem("Projetos", "/preview/projects", "projects"),
            new BrqNavigationItem("Marcos", "/milestones", "milestones")
        };

        var cut = Render<BrqAppShell>(parameters => parameters
            .Add(component => component.ProductName, "Operations PX")
            .Add(component => component.NavigationItems, items)
            .AddChildContent("<h1>Carteira</h1>"));

        Assert.NotNull(cut.Find("[data-brq-ui].app"));

        var skipLink = cut.Find("a.brq-skip-link");
        Assert.Equal("#brq-main-content", skipLink.GetAttribute("href"));
        Assert.Equal("Navegação principal", cut.Find("aside.app__sidebar").GetAttribute("aria-label"));
        Assert.NotNull(cut.Find("nav.side-nav"));
        Assert.NotNull(cut.Find("main#brq-main-content.app__content"));
        Assert.Contains("Operations PX", cut.Markup);
        Assert.Contains("Carteira", cut.Markup);

        var activeLink = cut.Find("a.side-nav__link[aria-current='page']");
        Assert.Contains("Projetos", activeLink.TextContent);
    }

    [Fact]
    public void Shell_should_expose_mobile_navigation_and_theme_controls_with_names()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .NavigateTo("/preview/projects");

        var cut = Render<BrqAppShell>(parameters => parameters
            .Add(component => component.NavigationItems,
                [new BrqNavigationItem("Projetos", "/preview/projects", "projects")])
            .AddChildContent("<h1>Carteira</h1>"));

        Assert.NotNull(cut.Find("button.topbar__toggle[aria-label='Abrir navegação']"));

        var themeToggle = cut.Find("button.side-foot__btn[aria-label='Ativar tema escuro']");
        Assert.Contains("Modo escuro", themeToggle.TextContent);

        cut.Find("button.topbar__toggle").Click();

        var mobileDialog = cut.Find("dialog.brq-mobile-dialog[data-open='true']");
        Assert.Equal("Navegação principal", mobileDialog.QuerySelector("h2")?.TextContent);
        Assert.NotNull(mobileDialog.QuerySelector("a.side-nav__link[aria-current='page']"));
    }

    private sealed class FakeThemeService : IThemeService
    {
        public ThemePreference Current { get; private set; } = ThemePreference.Light;

        public event Action? Changed;

        public ValueTask InitializeAsync() => ValueTask.CompletedTask;

        public ValueTask ToggleAsync()
        {
            Current = Current == ThemePreference.Light
                ? ThemePreference.Dark
                : ThemePreference.Light;
            Changed?.Invoke();
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
