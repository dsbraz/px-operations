using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Milestones;

public partial class MilestonesMonthView : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<DateOnly> CalendarDays { get; set; } = [];
    [Parameter, EditorRequired] public DateOnly CurrentMonth { get; set; }
    [Parameter, EditorRequired] public DateOnly Today { get; set; }
    [Parameter, EditorRequired] public string MonthLabel { get; set; } = string.Empty;
    [Parameter, EditorRequired] public Func<DateOnly, List<MilestoneResponse>> GetItems { get; set; } = _ => [];
    [Parameter, EditorRequired] public Func<string, string> TypeCss { get; set; } = _ => string.Empty;
    [Parameter] public EventCallback<int> OnShiftMonth { get; set; }
    [Parameter] public EventCallback<MilestoneResponse> OnOpenDetail { get; set; }
}
