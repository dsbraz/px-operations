using Microsoft.AspNetCore.Components;

namespace PxOperations.BlazorWasm.Features.Projects;

public partial class ProjectsToolbar : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<string> DeliveryCenters { get; set; } = [];
    [Parameter, EditorRequired] public IReadOnlyList<string> Statuses { get; set; } = [];
    [Parameter, EditorRequired] public IReadOnlyList<string> Types { get; set; } = [];
    [Parameter] public string SearchTerm { get; set; } = string.Empty;
    [Parameter] public string FilterDc { get; set; } = string.Empty;
    [Parameter] public string FilterStatus { get; set; } = string.Empty;
    [Parameter] public string FilterType { get; set; } = string.Empty;
    [Parameter] public string FilterRenewal { get; set; } = string.Empty;
    [Parameter] public string ActiveTab { get; set; } = "lista";
    [Parameter] public EventCallback<string> OnSearchTermChanged { get; set; }
    [Parameter] public EventCallback<string> OnFilterDcChanged { get; set; }
    [Parameter] public EventCallback<string> OnFilterStatusChanged { get; set; }
    [Parameter] public EventCallback<string> OnFilterTypeChanged { get; set; }
    [Parameter] public EventCallback<string> OnFilterRenewalChanged { get; set; }
    [Parameter] public EventCallback<string> OnActiveTabChanged { get; set; }

    private Task OnSearchChanged(ChangeEventArgs args) => OnSearchTermChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
    private Task OnDcChanged(ChangeEventArgs args) => OnFilterDcChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
    private Task OnStatusChanged(ChangeEventArgs args) => OnFilterStatusChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
    private Task OnTypeChanged(ChangeEventArgs args) => OnFilterTypeChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
    private Task OnRenewalChanged(ChangeEventArgs args) => OnFilterRenewalChanged.InvokeAsync(args.Value?.ToString() ?? string.Empty);
}
