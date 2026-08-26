using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Layout;
using PxOperations.Ui.Theming;

namespace PxOperations.BlazorWasm.Tests.Layout;

public sealed class MainLayoutTests : TestContext
{
    public MainLayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IThemeService, FakeThemeService>();
    }

    /// <summary>
    /// A barra lateral do protótipo lista os quatro módulos. Ela é do app: o
    /// NPS mora dentro dela, e as outras três telas entram na mesma moldura.
    /// </summary>
    [Fact]
    public void Shell_should_list_the_four_modules()
    {
        var cut = RenderComponent<MainLayout>();

        Assert.Contains("Projetos", cut.Markup);
        Assert.Contains("/milestones", cut.Markup);
        Assert.Contains("/project-health", cut.Markup);
        Assert.Contains("/nps", cut.Markup);
    }

    /// <summary>
    /// Só o NPS foi construído sobre o design system. Ligar o reset no conteúdo
    /// das telas legadas as reestilizaria inteiras — elas ganham a moldura nova,
    /// não uma aparência nova.
    /// </summary>
    [Theory]
    [InlineData("/", false)]
    [InlineData("/milestones", false)]
    [InlineData("/project-health", false)]
    [InlineData("/nps", true)]
    public void Content_should_only_opt_into_the_design_system_reset_on_nps(string path, bool expected)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(path);

        var cut = RenderComponent<MainLayout>();
        var content = cut.Find(".app__content-inner");

        Assert.Equal(expected, content.HasAttribute("data-brq-ui"));
    }

    private sealed class FakeThemeService : IThemeService
    {
        public ThemePreference Current => ThemePreference.Light;
        public event Action? Changed;
        public ValueTask InitializeAsync() { Changed?.Invoke(); return ValueTask.CompletedTask; }
        public ValueTask ToggleAsync() => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
