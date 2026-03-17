using System.Globalization;
using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Milestones;

public partial class MilestonesWeekView : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<DateOnly> WeekDays { get; set; } = [];
    [Parameter, EditorRequired] public string WeekLabel { get; set; } = string.Empty;
    [Parameter, EditorRequired] public DateOnly Today { get; set; }
    [Parameter, EditorRequired] public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;
    [Parameter, EditorRequired] public Func<DateOnly, List<MilestoneResponse>> GetItems { get; set; } = _ => [];
    [Parameter, EditorRequired] public Func<string, string> TypeCss { get; set; } = _ => string.Empty;
    [Parameter] public EventCallback<int> OnShiftWeek { get; set; }
    [Parameter] public EventCallback OnGoToday { get; set; }
    [Parameter] public EventCallback<MilestoneResponse> OnOpenDetail { get; set; }
}
