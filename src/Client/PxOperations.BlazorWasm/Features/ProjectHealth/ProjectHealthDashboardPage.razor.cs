using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.ProjectHealth;

public partial class ProjectHealthDashboardPage : ComponentBase
{
    [Inject] private ProjectHealthClient ProjectHealthClient { get; set; } = default!;

    private ProjectHealthSummaryResponse? summary;
    private List<ProjectHealthResponse> entries = [];
    private bool isLoading = true;

    private string searchTerm = "";
    private string filterDc = "";
    private string filterScore = "";
    private string activeTab = "dash";

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        isLoading = true;
        var dc = string.IsNullOrEmpty(filterDc) ? null : filterDc;
        int? minScore = filterScore switch { "hi" => 7, "md" => 4, "lo" => 0, _ => null };
        int? maxScore = filterScore switch { "hi" => 10, "md" => 6, "lo" => 3, _ => null };

        summary = await ProjectHealthClient.GetSummaryAsync(null, dc, null, null, minScore, maxScore, default);
        entries = (await ProjectHealthClient.List2Async(null, dc, null, null, minScore, maxScore, default)).ToList();
        isLoading = false;
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
    private async Task OnFilterDcChanged(string value) { filterDc = value; await LoadDataAsync(); }
    private async Task OnFilterScoreChanged(string value) { filterScore = value; await LoadDataAsync(); }
    private void OnTabChanged(string tab) => activeTab = tab;
}
