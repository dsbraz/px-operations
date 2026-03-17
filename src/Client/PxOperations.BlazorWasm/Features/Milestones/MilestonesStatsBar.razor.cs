using Microsoft.AspNetCore.Components;

namespace PxOperations.BlazorWasm.Features.Milestones;

public partial class MilestonesStatsBar : ComponentBase
{
    [Parameter] public int TotalCount { get; set; }
    [Parameter] public int WeekCount { get; set; }
    [Parameter] public int MonthCount { get; set; }
    [Parameter] public int SponsorCount { get; set; }
}
