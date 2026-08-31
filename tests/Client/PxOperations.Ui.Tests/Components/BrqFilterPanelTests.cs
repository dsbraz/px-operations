using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using PxOperations.Ui.Components.Forms;

namespace PxOperations.Ui.Tests.Components;

public sealed class BrqFilterPanelTests : BunitContext
{
    [Fact]
    public async Task Filter_panel_should_close_when_focus_leaves_the_popover()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = Render<BrqFilterPanel>(parameters => parameters
            .AddChildContent("<label>DC</label>"));

        cut.Find("button.fmenu__btn").Click();
        Assert.NotNull(cut.Find("section.fmenu__pop"));

        await cut.InvokeAsync(() => cut.Instance.CloseFromOutsideAsync());

        Assert.Empty(cut.FindAll("section.fmenu__pop"));
    }

    [Fact]
    public async Task Disposing_while_the_module_loads_should_not_attach_a_document_listener()
    {
        var jsRuntime = new DeferredModuleJsRuntime();
        Services.AddSingleton<IJSRuntime>(jsRuntime);

        var cut = Render<BrqFilterPanel>(parameters => parameters
            .AddChildContent("<label>DC</label>"));

        // Sai da página antes de o import terminar.
        await cut.Instance.DisposeAsync();
        jsRuntime.CompleteImport();
        await Task.Delay(100);

        Assert.DoesNotContain("attach", jsRuntime.Module.Invocations);
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
