using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Nps;

public partial class NpsResponsesView : ComponentBase
{
    [Parameter] public IReadOnlyList<NpsResponseView> Responses { get; set; } = [];
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public string? LoadError { get; set; }
    [Parameter] public EventCallback OnReload { get; set; }
    [Parameter] public EventCallback<NpsResponseView> OnOpenResponse { get; set; }

    private Task RowKeyDown(KeyboardEventArgs args, NpsResponseView response)
        => args.Key is "Enter" or " " ? OnOpenResponse.InvokeAsync(response) : Task.CompletedTask;
}
