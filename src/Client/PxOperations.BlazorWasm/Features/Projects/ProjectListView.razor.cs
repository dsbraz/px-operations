using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Projects;

public partial class ProjectListView : ComponentBase
{
    [Parameter, EditorRequired] public List<ProjectResponse> Projects { get; set; } = [];
    [Parameter, EditorRequired] public bool IsLoading { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback<int> OnEdit { get; set; }
    [Parameter] public EventCallback<int> OnDelete { get; set; }

    private bool tableOpen = true;
    private void ToggleTable() => tableOpen = !tableOpen;
}
