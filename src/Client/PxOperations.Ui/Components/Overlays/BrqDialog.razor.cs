using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PxOperations.Ui.Components.Overlays;

public partial class BrqDialog : ComponentBase, IAsyncDisposable
{
    private const string ModulePath =
        "./_content/PxOperations.Ui/Components/Overlays/BrqDialog.razor.js";

    private readonly string titleId = $"brq-dialog-title-{Guid.NewGuid():N}";
    private readonly string descriptionId = $"brq-dialog-description-{Guid.NewGuid():N}";

    private ElementReference dialogElement;
    private IJSObjectReference? module;
    private DotNetObjectReference<BrqDialog>? selfReference;
    private bool lastOpen;
    private bool disposed;

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;
    [Parameter] public string? Description { get; set; }
    [Parameter] public string? CssClass { get; set; }
    [Parameter] public string? AutofocusSelector { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? Footer { get; set; }
    [Parameter] public EventCallback OnClosed { get; set; }

    private string DialogClass => string.IsNullOrWhiteSpace(CssClass)
        ? "brq-dialog"
        : $"brq-dialog {CssClass}";

    private string? DescriptionId => string.IsNullOrWhiteSpace(Description)
        ? null
        : descriptionId;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (disposed || (!Open && module is null))
        {
            lastOpen = Open;
            return;
        }

        if (module is null)
        {
            var loaded = await JSRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);

            // O import é assíncrono: ser descartado no meio dele já rodou o
            // DisposeAsync, que não tinha módulo para soltar. Sem esta guarda a
            // continuação registrava os listeners de close e cancel num diálogo
            // já desconectado e chamava showModal num elemento fora do
            // documento, que lança.
            if (disposed)
            {
                try { await loaded.DisposeAsync(); }
                catch (JSDisconnectedException) { }
                return;
            }

            module = loaded;
            selfReference = DotNetObjectReference.Create(this);
        }

        if (Open != lastOpen || firstRender)
        {
            await module.InvokeVoidAsync(
                "sync",
                dialogElement,
                Open,
                selfReference,
                AutofocusSelector);
            lastOpen = Open;
        }
    }

    [JSInvokable]
    public async Task NotifyNativeCloseAsync()
    {
        if (!Open)
            return;

        await OpenChanged.InvokeAsync(false);
        await OnClosed.InvokeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        disposed = true;
        if (module is not null)
        {
            try
            {
                await module.InvokeVoidAsync("dispose", dialogElement);
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        selfReference?.Dispose();
    }

    private async Task RequestCloseAsync()
    {
        await OpenChanged.InvokeAsync(false);
        await OnClosed.InvokeAsync();
    }
}
