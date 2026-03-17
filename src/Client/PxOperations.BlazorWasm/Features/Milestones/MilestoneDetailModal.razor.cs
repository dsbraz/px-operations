using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Milestones;

public partial class MilestoneDetailModal : ComponentBase
{
    [Parameter, EditorRequired] public MilestoneResponse Milestone { get; set; } = default!;
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback OnDelete { get; set; }
}
