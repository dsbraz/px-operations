using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using PxOperations.Ui.Components.Navigation;
using PxOperations.Ui.Theming;

namespace PxOperations.Ui.Tests.Components;

public sealed class BrqThemeToggleTests : BunitContext
{
    public BrqThemeToggleTests()
    {
        Services.AddSingleton<IThemeService>(new FailingThemeService());
    }

    [Fact]
    public void Toggle_should_stay_usable_when_the_theme_module_fails_to_load()
    {
        var cut = Render<BrqThemeToggle>();

        Assert.NotNull(cut.Find("button.side-foot__btn[aria-label='Ativar tema escuro']"));
    }

    [Fact]
    public void Toggle_should_stay_usable_when_switching_the_theme_fails()
    {
        var cut = Render<BrqThemeToggle>();

        cut.Find("button.side-foot__btn").Click();

        Assert.NotNull(cut.Find("button.side-foot__btn[aria-label='Ativar tema escuro']"));
    }

    private sealed class FailingThemeService : IThemeService
    {
        public ThemePreference Current => ThemePreference.Light;

        public event Action? Changed
        {
            add { }
            remove { }
        }

        public ValueTask InitializeAsync() =>
            ValueTask.FromException(new JSException("Falha ao importar theme.js."));

        public ValueTask ToggleAsync() =>
            ValueTask.FromException(new JSException("Falha ao importar theme.js."));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
