using Microsoft.AspNetCore.Components;

namespace PxOperations.BlazorWasm.Features.Nps;

public partial class NpsWaiverModal : ComponentBase
{
    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter] public string Reason { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> ReasonChanged { get; set; }
    [Parameter] public string? ActionError { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback OnConfirm { get; set; }
}
