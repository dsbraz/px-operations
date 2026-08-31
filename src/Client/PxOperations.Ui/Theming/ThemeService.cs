using Microsoft.JSInterop;

namespace PxOperations.Ui.Theming;

public sealed class ThemeService(IJSRuntime jsRuntime) : IThemeService
{
    private const string ImportIdentifier = "import";
    private const string ModulePath = "./_content/PxOperations.Ui/js/theme.js";

    private IJSObjectReference? module;
    private Task? initialization;
    private bool disposed;

    public ThemePreference Current { get; private set; } = ThemePreference.Light;

    public event Action? Changed;

    public ValueTask InitializeAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var pending = initialization ??= InitializeCoreAsync();
        return new ValueTask(ForgetIfItFailsAsync(pending));
    }

    // Uma falha transitória ao importar o módulo não pode desabilitar o tema
    // para o resto da sessão: a task com falha ficaria cacheada em
    // `initialization` e toda chamada seguinte relançaria a mesma exceção.
    //
    // A limpeza precisa acontecer aqui, e não dentro de InitializeCoreAsync:
    // quando a interop rejeita antes do primeiro await, o catch de lá rodava e
    // zerava o campo ANTES de o `??=` acima gravar a task já falha — ou seja,
    // justamente no caso que queríamos proteger, o cache voltava.
    private async Task ForgetIfItFailsAsync(Task pending)
    {
        try
        {
            await pending;
        }
        catch
        {
            if (ReferenceEquals(initialization, pending))
            {
                initialization = null;
            }

            throw;
        }
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
        var loaded = await jsRuntime.InvokeAsync<IJSObjectReference>(ImportIdentifier, ModulePath);

        // Uma tentativa anterior pode ter importado o módulo e falhado só no
        // getTheme; trocar a referência sem soltar a antiga vazava o objeto no
        // runtime JS a cada retentativa.
        if (module is not null)
        {
            await module.DisposeAsync();
        }

        module = loaded;
        Current = Parse(await module.InvokeAsync<string>("getTheme"));
        Changed?.Invoke();
    }

    private static ThemePreference Parse(string? value) =>
        string.Equals(value, "dark", StringComparison.Ordinal)
            ? ThemePreference.Dark
            : ThemePreference.Light;
}
