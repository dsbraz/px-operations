using Microsoft.AspNetCore.Components;

namespace PxOperations.BlazorWasm.Features.ProjectHealth;

public partial class ProjectHealthStatsBar : ComponentBase
{
    [Parameter] public int? TotalProjects { get; set; }
    [Parameter] public double? AverageScore { get; set; }
    [Parameter] public int? CriticalCount { get; set; }
    [Parameter] public int? NoResponseCount { get; set; }
}
