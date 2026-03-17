using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Milestones;

public partial class MilestonesToolbar : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<string> DeliveryCenters { get; set; } = [];
    [Parameter, EditorRequired] public IReadOnlyList<string> MilestoneTypes { get; set; } = [];
    [Parameter, EditorRequired] public IReadOnlyList<ProjectResponse> Projects { get; set; } = [];
    [Parameter] public string SearchTerm { get; set; } = string.Empty;
    [Parameter] public string FilterDc { get; set; } = string.Empty;
    [Parameter] public string FilterType { get; set; } = string.Empty;
    [Parameter] public string FilterProjectId { get; set; } = string.Empty;
    [Parameter] public string ActiveView { get; set; } = "semana";
    [Parameter] public Func<string, string> TypeCss { get; set; } = _ => string.Empty;
    [Parameter] public EventCallback<string> OnSearchTermChanged { get; set; }
    [Parameter] public EventCallback<string> OnFilterDcChanged { get; set; }
    [Parameter] public EventCallback<string> OnFilterTypeChanged { get; set; }
    [Parameter] public EventCallback<string> OnFilterProjectIdChanged { get; set; }
    [Parameter] public EventCallback<string> OnViewChanged { get; set; }

    private Task OnSearchChanged(ChangeEventArgs args) => OnSearchTermChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
    private Task OnDcChanged(ChangeEventArgs args) => OnFilterDcChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
    private Task OnTypeChanged(ChangeEventArgs args) => OnFilterTypeChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
    private Task OnProjectChanged(ChangeEventArgs args) => OnFilterProjectIdChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
}
