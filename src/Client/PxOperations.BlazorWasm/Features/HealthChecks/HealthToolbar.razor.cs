using Microsoft.AspNetCore.Components;

namespace PxOperations.BlazorWasm.Features.HealthChecks;

public partial class HealthToolbar : ComponentBase
{
    [Parameter] public string SearchTerm { get; set; } = "";
    [Parameter] public EventCallback<string> OnSearchChanged { get; set; }
    [Parameter] public string FilterDc { get; set; } = "";
    [Parameter] public EventCallback<string> OnFilterDcChanged { get; set; }
    [Parameter] public string FilterScore { get; set; } = "";
    [Parameter] public EventCallback<string> OnFilterScoreChanged { get; set; }
    [Parameter] public string ActiveTab { get; set; } = "dash";
    [Parameter] public EventCallback<string> OnTabChanged { get; set; }
}
