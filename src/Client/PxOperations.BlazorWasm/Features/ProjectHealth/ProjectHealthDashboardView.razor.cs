using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.ProjectHealth;

public partial class ProjectHealthDashboardView : ComponentBase
{
    [Parameter, EditorRequired] public ProjectHealthSummaryResponse? Summary { get; set; }
    [Parameter, EditorRequired] public List<ProjectHealthResponse> Entries { get; set; } = [];
    [Parameter] public EventCallback<ProjectHealthResponse> OnCardClick { get; set; }
}
