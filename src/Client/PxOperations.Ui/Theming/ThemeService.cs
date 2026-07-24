using Microsoft.JSInterop;

namespace PxOperations.Ui.Theming;

public sealed class ThemeService(IJSRuntime jsRuntime) : IThemeService
{
    private const string ModulePath = "./_content/PxOperations.Ui/js/theme.js";

    private IJSObjectReference? module;
    private Task? initialization;
    private bool disposed;

    public ThemePreference Current { get; private set; } = ThemePreference.Light;

    public event Action? Changed;

    public ValueTask InitializeAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        initialization ??= InitializeCoreAsync();
        return new ValueTask(initialization);
    }

    public async ValueTask ToggleAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await InitializeAsync();

        var next = Current == ThemePreference.Light
            ? ThemePreference.Dark
            : ThemePreference.Light;

        var resolved = await module!.InvokeAsync<string>(
            "setTheme",
            next == ThemePreference.Dark ? "dark" : "light");

        Current = Parse(resolved);
        Changed?.Invoke();
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        disposed = true;
        if (module is not null)
            await module.DisposeAsync();
    }

    private async Task InitializeCoreAsync()
    {
        module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
        Current = Parse(await module.InvokeAsync<string>("getTheme"));
        Changed?.Invoke();
    }

    private static ThemePreference Parse(string? value) =>
        string.Equals(value, "dark", StringComparison.Ordinal)
            ? ThemePreference.Dark
            : ThemePreference.Light;
}
