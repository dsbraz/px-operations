using Microsoft.JSInterop;
using PxOperations.Ui.Theming;

namespace PxOperations.Ui.Tests.Theming;

public sealed class ThemeServiceTests
{
    /// <summary>
    /// Uma falha ao importar o módulo precisa continuar retentável: se a task
    /// com falha ficar cacheada, o tema morre para o resto da sessão.
    /// </summary>
    [Fact]
    public async Task A_failed_initialization_should_stay_retryable()
    {
        var jsRuntime = new FlakyJsRuntime();
        await using var service = new ThemeService(jsRuntime);

        await Assert.ThrowsAsync<JSException>(async () => await service.InitializeAsync());
        await service.InitializeAsync();

        Assert.Equal(ThemePreference.Dark, service.Current);
        Assert.Equal(2, jsRuntime.ImportCount);
    }

    private sealed class FlakyJsRuntime : IJSRuntime
    {
        public int ImportCount { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            if (identifier != "import")
            {
                return ValueTask.FromResult(default(TValue)!);
            }

            ImportCount++;
            if (ImportCount == 1)
            {
                // Rejeição síncrona: é o caso que o ??= com a chamada embutida
                // não protege, porque o catch zera o campo antes da atribuição.
                throw new JSException("Falha ao importar theme.js.");
            }

            return ValueTask.FromResult((TValue)(object)new DarkThemeModule());
        }
    }

    private sealed class DarkThemeModule : IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
            => ValueTask.FromResult((TValue)(object)"dark");

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
