using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Nps;

public partial class NpsResponseModal : ComponentBase
{
    [Parameter] public bool Open { get; set; }
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter] public NpsResponseView? Response { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
}
