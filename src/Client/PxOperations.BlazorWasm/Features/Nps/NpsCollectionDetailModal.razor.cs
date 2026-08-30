using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Nps;

public partial class NpsCollectionDetailModal : ComponentBase
{
    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter] public NpsProjectDetailView? Detail { get; set; }
    [Parameter] public IReadOnlyList<NpsResponseView> Responses { get; set; } = [];

    /// <summary>Formato selecionado no segmentado: "all", "complete" ou "simplified".</summary>
    [Parameter] public string Format { get; set; } = "all";

    [Parameter] public EventCallback<string> OnFilter { get; set; }
    [Parameter] public string? ActionError { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
}
