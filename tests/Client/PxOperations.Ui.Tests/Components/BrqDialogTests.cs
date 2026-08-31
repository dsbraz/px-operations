using Bunit;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.Ui.Components.Overlays;

namespace PxOperations.Ui.Tests.Components;

public sealed class BrqDialogTests : BunitContext
{
    [Fact]
    public void Dialog_should_use_native_element_and_accessible_title()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = Render<BrqDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Detalhes do projeto")
            .AddChildContent("<p>Projeto Alpha</p>"));

        var dialog = cut.Find("dialog");
        var title = cut.Find("h2");

        Assert.Equal(title.Id, dialog.GetAttribute("aria-labelledby"));
        Assert.Equal("true", dialog.GetAttribute("data-open"));
        Assert.Contains("Projeto Alpha", dialog.TextContent);
        Assert.NotNull(cut.Find("button[aria-label='Fechar diálogo']"));
    }

    [Fact]
    public void Close_button_should_notify_the_owner()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        bool? open = true;
        var closed = false;

        var cut = Render<BrqDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Detalhes")
            .Add(component => component.OpenChanged,
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create<bool>(
                    this, value => open = value))
            .Add(component => component.OnClosed,
                Microsoft.AspNetCore.Components.EventCallback.Factory.Create(
                    this, () => closed = true)));

        cut.Find("button[aria-label='Fechar diálogo']").Click();

        Assert.False(open);
        Assert.True(closed);
    }

    [Fact]
    public async Task Disposing_while_the_module_loads_should_not_touch_a_detached_dialog()
    {
        var jsRuntime = new DeferredModuleJsRuntime();
        Services.AddSingleton<IJSRuntime>(jsRuntime);

        var cut = Render<BrqDialog>(parameters => parameters
            .Add(dialog => dialog.Title, "Título")
            .Add(dialog => dialog.Open, true));

        // Sai da página antes de o import terminar.
        await cut.Instance.DisposeAsync();
        jsRuntime.CompleteImport();
        await Task.Delay(100);

        Assert.DoesNotContain("sync", jsRuntime.Module.Invocations);
    }

    private sealed class DeferredModuleJsRuntime : IJSRuntime
    {
        private readonly TaskCompletionSource<IJSObjectReference> import = new();

        public RecordingModule Module { get; } = new();

        public void CompleteImport() => import.SetResult(Module);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public async ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            if (identifier == "import")
            {
                return (TValue)await import.Task;
            }

            return default!;
        }
    }

    private sealed class RecordingModule : IJSObjectReference
    {
        public List<string> Invocations { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Invocations.Add(identifier);
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Invocations.Add(identifier);
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
