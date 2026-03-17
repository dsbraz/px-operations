using Microsoft.AspNetCore.Components;

namespace PxOperations.BlazorWasm.Features.Projects;

public partial class ProjectsStatsBar : ComponentBase
{
    [Parameter] public int TotalCount { get; set; }
    [Parameter] public int ActiveCount { get; set; }
    [Parameter] public int ClientCount { get; set; }
    [Parameter] public int ExpiringIn60DaysCount { get; set; }
    [Parameter] public int RenewingCount { get; set; }
    [Parameter] public int ApprovedRenewalCount { get; set; }
}
