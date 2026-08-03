namespace PxOperations.Ui.Theming;

public interface IThemeService : IAsyncDisposable
{
    ThemePreference Current { get; }

    event Action? Changed;

    ValueTask InitializeAsync();

    ValueTask ToggleAsync();
}
