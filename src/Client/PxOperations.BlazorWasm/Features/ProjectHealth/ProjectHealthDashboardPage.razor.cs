using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.ProjectHealth;

public partial class ProjectHealthDashboardPage : ComponentBase
{
    [Inject] private ProjectHealthClient ProjectHealthClient { get; set; } = default!;

    private ProjectHealthSummaryResponse? summary;
    private List<ProjectHealthResponse> entries = [];
    private bool isLoading = true;
    private string? loadError;

    private string searchTerm = "";
    private string filterDc = "";
    private string filterScore = "";
    private string filterWeek = "";
    private string activeTab = "dash";

    // Cached at the end of LoadDataAsync; recomputing per render would allocate a new list each diff pass.
    private IReadOnlyList<string> availableWeeks = [];

    private bool showDetailModal;
    private ProjectHealthResponse? detailEntry;

    private void OpenDetailModal(ProjectHealthResponse entry) { detailEntry = entry; showDetailModal = true; }
    private void CloseDetailModal() { showDetailModal = false; detailEntry = null; }

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    // Full reload: summary + list. Used when the period scope (DC or week) changes.
    private async Task LoadDataAsync()
    {
        isLoading = true;
        loadError = null;

        try
        {
            var dc = string.IsNullOrEmpty(filterDc) ? null : filterDc;
            var week = string.IsNullOrEmpty(filterWeek) ? null : filterWeek;

            // Summary represents the active carteira for the period: scoped only by DC and week.
            // Busca/Nota narrow the lists below, not the headline truths.
            summary = await ProjectHealthClient.GetSummaryAsync(null, dc, null, week, null, null, default);
            availableWeeks = summary?.WeeklyEvolution?.Select(w => w.Week).Reverse().ToList() ?? [];
            await FetchEntriesAsync(dc, week);
        }
        catch (Exception)
        {
            loadError = "Não foi possível carregar os dados. Tente recarregar a página.";
        }
        finally
        {
            isLoading = false;
        }
    }

    // List-only reload: the Nota filter narrows the lists but leaves the summary truths unchanged.
    private async Task ReloadEntriesAsync()
    {
        isLoading = true;
        loadError = null;

        try
        {
            var dc = string.IsNullOrEmpty(filterDc) ? null : filterDc;
            var week = string.IsNullOrEmpty(filterWeek) ? null : filterWeek;
            await FetchEntriesAsync(dc, week);
        }
        catch (Exception)
        {
            loadError = "Não foi possível carregar os dados. Tente recarregar a página.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task FetchEntriesAsync(string? dc, string? week)
    {
        int? minScore = filterScore switch { "hi" => 7, "md" => 4, "lo" => 0, _ => null };
        int? maxScore = filterScore switch { "hi" => 10, "md" => 6, "lo" => 3, _ => null };
        entries = (await ProjectHealthClient.List2Async(null, dc, null, week, minScore, maxScore, default)).ToList();
    }

    private List<ProjectHealthResponse> FilteredEntries
    {
        get
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return entries;
            var term = searchTerm.ToLowerInvariant();
            return entries.Where(e =>
                e.ProjectName.ToLowerInvariant().Contains(term) ||
                (e.ProjectClient?.ToLowerInvariant().Contains(term) ?? false) ||
                (e.SubProject?.ToLowerInvariant().Contains(term) ?? false) ||
                e.Highlights.ToLowerInvariant().Contains(term)).ToList();
        }
    }

    private void OnSearchChanged(string value) => searchTerm = value;
    // Changing the DC re-scopes the carteira, so the available weeks change too — reset to "Última semana".
    private async Task OnFilterDcChanged(string value) { filterDc = value; filterWeek = ""; await LoadDataAsync(); }
    private async Task OnFilterScoreChanged(string value) { filterScore = value; await ReloadEntriesAsync(); }
    private async Task OnFilterWeekChanged(string value) { filterWeek = value; await LoadDataAsync(); }
    private void OnTabChanged(string tab) => activeTab = tab;
}
