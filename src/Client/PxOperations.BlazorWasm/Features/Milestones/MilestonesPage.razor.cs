using System.Globalization;
using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Milestones;

public partial class MilestonesPage : ComponentBase
{
    private static readonly string[] deliveryCenters = ["DC1", "DC2", "DC3", "DC4", "DC5", "DC6"];
    private static readonly string[] milestoneTypes =
    [
        "Apresentação Sponsor",
        "Entrega Final",
        "Presencial com Cliente",
        "Kickoff",
        "Outros"
    ];

    [Inject] private MilestonesClient MilestonesClient { get; set; } = default!;
    [Inject] private ProjectsClient ProjectsClient { get; set; } = default!;

    private readonly CultureInfo portugueseCulture = CultureInfo.GetCultureInfo("pt-BR");
    private readonly DateOnly today = DateOnly.FromDateTime(DateTime.Today);
    private bool isLoading = true;
    private string? errorMessage;
    private List<MilestoneResponse> milestones = [];
    private List<ProjectResponse> projects = [];

    private string searchTerm = string.Empty;
    private string filterDc = string.Empty;
    private string filterType = string.Empty;
    private string filterProjectId = string.Empty;
    private string activeView = "semana";
    private DateOnly currentWeekStart = StartOfWeek(DateOnly.FromDateTime(DateTime.Today));
    private DateOnly currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    private bool showModal;
    private MilestoneResponse? editingMilestone;
    private MilestoneResponse? selectedMilestone;
    private string? toastMessage;
    private CancellationTokenSource? toastCts;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var milestonesResult = await MilestonesClient.ListAsync(null, null, null, null, null, null, default);
            var projectsResult = await ProjectsClient.ListAsync(null, null, null, null, null, default);

            milestones = milestonesResult.ToList();
            projects = projectsResult.OrderBy(p => p.Name).ToList();
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isLoading = false;
        }
    }

    private List<MilestoneResponse> FilteredMilestones => milestones
        .Where(m => string.IsNullOrWhiteSpace(searchTerm)
            || m.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            || m.ProjectName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        .Where(m => string.IsNullOrEmpty(filterDc) || m.ProjectDc == filterDc)
        .Where(m => string.IsNullOrEmpty(filterType) || m.Type == filterType)
        .Where(m => string.IsNullOrEmpty(filterProjectId) || m.ProjectId.ToString() == filterProjectId)
        .OrderBy(m => m.Date)
        .ThenBy(m => m.Time)
        .ThenBy(m => m.Title)
        .ToList();

    private int ThisWeekCount => FilteredMilestones.Count(m => ParseDate(m.Date) >= currentWeekStart && ParseDate(m.Date) <= currentWeekStart.AddDays(6));
    private int ThisMonthCount => FilteredMilestones.Count(m => ParseDate(m.Date).Month == currentMonth.Month && ParseDate(m.Date).Year == currentMonth.Year);
    private int SponsorCount => FilteredMilestones.Count(m => m.Type == "Apresentação Sponsor");
    private string WeekLabel => $"{currentWeekStart:dd/MM} - {currentWeekStart.AddDays(4):dd/MM}";
    private string MonthLabel => currentMonth.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy", portugueseCulture);
    private IReadOnlyList<DateOnly> WeekDays => Enumerable.Range(0, 5).Select(offset => currentWeekStart.AddDays(offset)).ToList();
    private IReadOnlyList<DateOnly> CalendarDays => BuildCalendarDays();

    private void OpenCreateModal()
    {
        editingMilestone = null;
        showModal = true;
    }

    private void OpenDetail(MilestoneResponse milestone)
    {
        selectedMilestone = milestone;
    }

    private void CloseDetail()
    {
        selectedMilestone = null;
    }

    private void EditSelected()
    {
        editingMilestone = selectedMilestone;
        CloseDetail();
        showModal = true;
    }

    private async Task DeleteSelectedAsync()
    {
        if (selectedMilestone is null)
            return;

        try
        {
            await MilestonesClient.DeleteAsync(selectedMilestone.Id, default);
            milestones.RemoveAll(m => m.Id == selectedMilestone.Id);
            CloseDetail();
            await ShowToast("Marco removido.");
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
    }

    private void CloseModal()
    {
        showModal = false;
        editingMilestone = null;
    }

    private async Task HandleSavedAsync(MilestoneResponse milestone)
    {
        var index = milestones.FindIndex(m => m.Id == milestone.Id);
        if (index >= 0) milestones[index] = milestone;
        else milestones.Add(milestone);

        CloseModal();
        await ShowToast("Marco salvo com sucesso.");
    }

    private List<MilestoneResponse> WeekItems(DateOnly day) =>
        FilteredMilestones.Where(m => ParseDate(m.Date) == day).ToList();

    private List<MilestoneResponse> CalendarItems(DateOnly day) =>
        FilteredMilestones.Where(m => ParseDate(m.Date) == day).ToList();

    private void ShiftWeek(int offset) => currentWeekStart = currentWeekStart.AddDays(offset * 7);
    private void ShiftMonth(int offset) => currentMonth = currentMonth.AddMonths(offset);

    private void GoToday()
    {
        currentWeekStart = StartOfWeek(DateOnly.FromDateTime(DateTime.Today));
        currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    }

    private IReadOnlyList<DateOnly> BuildCalendarDays()
    {
        var firstDay = currentMonth;
        var start = firstDay.AddDays(-(int)firstDay.DayOfWeek);
        return Enumerable.Range(0, 42).Select(offset => start.AddDays(offset)).ToList();
    }

    private static DateOnly ParseDate(string value) => DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var diff = (7 + ((int)date.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff);
    }

    private string TypeCss(string type) => type switch
    {
        "Apresentação Sponsor" => "tb-sponsor",
        "Entrega Final" => "tb-entrega",
        "Presencial com Cliente" => "tb-presencial",
        "Kickoff" => "tb-kickoff",
        _ => "tb-outros"
    };

    private async Task ShowToast(string message)
    {
        toastCts?.Cancel();
        toastCts = new CancellationTokenSource();
        toastMessage = message;
        StateHasChanged();

        try
        {
            await Task.Delay(3000, toastCts.Token);
            toastMessage = null;
            StateHasChanged();
        }
        catch (TaskCanceledException)
        {
        }
    }
}
