using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Projects.Preview;

internal enum PreviewFacet
{
    DeliveryCenter,
    Status,
    Type,
    Renewal
}

public partial class ProjectsPreviewPage : ComponentBase, IDisposable
{
    internal static readonly string[] DeliveryCenterOptions = ["DC1", "DC2", "DC3", "DC4", "DC5", "DC6"];
    internal static readonly string[] StatusOptions = ["Em andamento", "Programado", "Encerrado"];
    internal static readonly string[] TypeOptions = ["Squad", "Escopo Fechado", "Alocação"];
    internal static readonly string[] RenewalOptions = ["Aprovada", "Em andamento", "Pendente", "None"];

    private readonly List<ProjectResponse> projects = [];
    private CancellationTokenSource? loadCancellation;
    private CancellationTokenSource? toastCts;
    private ProjectsPreviewFilterState filterState = ProjectsPreviewFilterState.Empty;
    private ProjectResponse? editingProject;
    private ProjectResponse? pendingDeleteProject;
    private string activeTab = "lista";
    private string renewalYear = "2026";
    private string renewalPeriod = "ano";
    private string? toastMessage;
    private bool isLoading = true;
    private bool hasLoadError;
    private bool isDeleting;
    private bool showModal;
    private bool disposed;

    private bool IsManagementView => !NavigationManager.ToBaseRelativePath(NavigationManager.Uri)
        .Split('?', 2)[0]
        .TrimEnd('/')
        .EndsWith("/dashboard", StringComparison.OrdinalIgnoreCase);

    [Inject] private ProjectsClient ProjectsClient { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private List<ProjectResponse> FilteredProjects => projects
        .Where(MatchesSearch)
        .Where(project => MatchesFacet(filterState.DeliveryCenters, project.Dc))
        .Where(project => MatchesFacet(filterState.Statuses, project.Status))
        .Where(project => MatchesFacet(filterState.Types, project.Type))
        .Where(project => MatchesFacet(filterState.Renewals, project.Renewal))
        .ToList();

    private int ActiveCount => projects.Count(p => p.Status == "Em andamento");
    private int ClientCount => projects.Select(p => p.Client).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
    private int RenewingCount => projects.Count(p => p.Renewal == "Em andamento");
    private int ApprovedRenewalCount => projects.Count(p => p.Renewal == "Aprovada");
    private int ExpiringSoonCount => projects.Count(p =>
        DateTime.TryParse(p.EndDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end)
        && (end.Date - DateTime.Today).Days is >= 0 and <= 60);

    private List<ProjectResponse> NewScheduledProjects =>
        projects.Where(project => project.Status == "Programado").ToList();

    private List<ProjectResponse> StartedLastWeekProjects =>
        ProjectsWithin(project => project.StartDate, PreviousWeekStart, PreviousWeekEnd);

    private List<ProjectResponse> EndedLastWeekProjects =>
        ProjectsWithin(project => project.EndDate, PreviousWeekStart, PreviousWeekEnd);

    private List<ProjectResponse> ApprovedProjects =>
        projects.Where(project => project.Renewal == "Aprovada").ToList();

    private static DateTime CurrentWeekStart
    {
        get
        {
            var today = DateTime.Today;
            var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            return today.AddDays(-daysSinceMonday).Date;
        }
    }

    private static DateTime CurrentWeekEnd => CurrentWeekStart.AddDays(6);
    private static DateTime PreviousWeekStart => CurrentWeekStart.AddDays(-7);
    private static DateTime PreviousWeekEnd => CurrentWeekStart.AddDays(-1);
    private static string CurrentWeekLabel => $"{CurrentWeekStart:dd/MM} – {CurrentWeekEnd:dd/MM}";

    private int ActiveFacetCount =>
        (filterState.DeliveryCenters.Count > 0 ? 1 : 0)
        + (filterState.Statuses.Count > 0 ? 1 : 0)
        + (filterState.Types.Count > 0 ? 1 : 0)
        + (filterState.Renewals.Count > 0 ? 1 : 0);

    private int FilterPanelActiveCount => ActiveFacetCount
        + (activeTab == "renewals" && renewalYear != "2026" ? 1 : 0)
        + (activeTab == "renewals" && renewalPeriod != "ano" ? 1 : 0);

    private int ActiveFilterCount => filterState.DeliveryCenters.Count + filterState.Statuses.Count
        + filterState.Types.Count + filterState.Renewals.Count
        + (string.IsNullOrWhiteSpace(filterState.Search) ? 0 : 1);

    private string ResultsAnnouncement => FilteredProjects.Count == 1
        ? "1 projeto encontrado"
        : $"{FilteredProjects.Count} projetos encontrados";

    private IEnumerable<(string Key, string Value, Action Clear)> ActiveFilterTokens
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(filterState.Search))
                yield return ("Busca", filterState.Search, ClearSearch);
            foreach (var value in filterState.DeliveryCenters)
                yield return ("DC", value, () => RemoveFacet(PreviewFacet.DeliveryCenter, value));
            foreach (var value in filterState.Statuses)
                yield return ("Status", value, () => RemoveFacet(PreviewFacet.Status, value));
            foreach (var value in filterState.Types)
                yield return ("Tipo", value, () => RemoveFacet(PreviewFacet.Type, value));
            foreach (var value in filterState.Renewals)
                yield return ("Renovação", RenewalOptionLabel(value), () => RemoveFacet(PreviewFacet.Renewal, value));
        }
    }

    protected override async Task OnInitializedAsync()
    {
        filterState = ParseCurrentLocation();
        NavigationManager.LocationChanged += HandleLocationChanged;
        await LoadProjectsAsync();
    }

    private async Task LoadProjectsAsync()
    {
        loadCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        loadCancellation = cancellation;
        isLoading = true;
        hasLoadError = false;

        try
        {
            var response = await ProjectsClient.ListAsync(null, null, null, null, null, cancellation.Token);
            if (cancellation.IsCancellationRequested || disposed) return;
            projects.Clear();
            projects.AddRange(response);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch
        {
            if (!cancellation.IsCancellationRequested && !disposed)
            {
                // The preview remains usable when the API is not running locally.
                // Production pages still surface the error; this route is intentionally self-contained.
                projects.Clear();
                projects.AddRange(CreatePreviewProjects());
                hasLoadError = false;
            }
        }
        finally
        {
            if (ReferenceEquals(loadCancellation, cancellation))
            {
                loadCancellation = null;
                isLoading = false;
            }
            cancellation.Dispose();
        }
    }

    private bool MatchesSearch(ProjectResponse project)
    {
        var search = filterState.Search;
        return string.IsNullOrWhiteSpace(search)
            || project.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
            || (project.Client?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static bool MatchesFacet(IReadOnlyList<string> selected, string? value) =>
        selected.Count == 0 || selected.Contains(value ?? string.Empty, StringComparer.Ordinal);

    private void HandleSearchInput(ChangeEventArgs args) => CommitFilterState(filterState with
    {
        Search = args.Value?.ToString()?.TrimStart() ?? string.Empty
    });

    private void ToggleFacet(PreviewFacet facet, string option, ChangeEventArgs args)
    {
        var selected = args.Value is true || bool.TryParse(args.Value?.ToString(), out var parsed) && parsed;
        var values = FacetValues(facet).ToHashSet(StringComparer.Ordinal);
        if (selected) values.Add(option); else values.Remove(option);
        SetFacet(facet, values);
    }

    private void RemoveFacet(PreviewFacet facet, string option) => SetFacet(facet, FacetValues(facet).Where(value => value != option));

    private void SetFacet(PreviewFacet facet, IEnumerable<string> values)
    {
        var normalized = values.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        CommitFilterState(facet switch
        {
            PreviewFacet.DeliveryCenter => filterState with { DeliveryCenters = normalized },
            PreviewFacet.Status => filterState with { Statuses = normalized },
            PreviewFacet.Type => filterState with { Types = normalized },
            PreviewFacet.Renewal => filterState with { Renewals = normalized },
            _ => filterState
        });
    }

    private IReadOnlyList<string> FacetValues(PreviewFacet facet) => facet switch
    {
        PreviewFacet.DeliveryCenter => filterState.DeliveryCenters,
        PreviewFacet.Status => filterState.Statuses,
        PreviewFacet.Type => filterState.Types,
        PreviewFacet.Renewal => filterState.Renewals,
        _ => []
    };

    private void ClearSearch() => CommitFilterState(filterState with { Search = string.Empty });
    private void ClearFacets()
    {
        renewalYear = "2026";
        renewalPeriod = "ano";
        CommitFilterState(filterState with { DeliveryCenters = [], Statuses = [], Types = [], Renewals = [] });
    }

    private void ClearAllFilters()
    {
        renewalYear = "2026";
        renewalPeriod = "ano";
        CommitFilterState(ProjectsPreviewFilterState.Empty);
    }
    private void SetActiveTab(string tab) => activeTab = tab;

    private void HandleRenewalYearChanged(ChangeEventArgs args) =>
        renewalYear = args.Value?.ToString() ?? "2026";

    private void HandleRenewalPeriodChanged(ChangeEventArgs args) =>
        renewalPeriod = args.Value?.ToString() ?? "ano";

    private void CommitFilterState(ProjectsPreviewFilterState next)
    {
        filterState = next;
        var target = ProjectsPreviewQueryCodec.Build(next);
        var current = "/" + NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        if (!string.Equals(current, target, StringComparison.Ordinal)) NavigationManager.NavigateTo(target, replace: true);
    }

    private ProjectsPreviewFilterState ParseCurrentLocation() => ProjectsPreviewQueryCodec.Parse(new Uri(NavigationManager.Uri).Query);

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        filterState = ProjectsPreviewQueryCodec.Parse(new Uri(args.Location).Query);
        _ = InvokeAsync(StateHasChanged);
    }

    private void OpenCreateModal() { editingProject = null; showModal = true; }
    private void OpenEditModal(int id) { editingProject = projects.FirstOrDefault(p => p.Id == id); showModal = true; }
    private void CloseModal() { showModal = false; editingProject = null; }

    private async Task HandleProjectSaved(ProjectResponse saved)
    {
        var index = projects.FindIndex(project => project.Id == saved.Id);
        if (index >= 0) projects[index] = saved; else projects.Add(saved);
        CloseModal();
        await ShowToast(index >= 0 ? "Projeto atualizado com sucesso!" : "Projeto criado com sucesso!");
    }

    private void HandleProjectUpdated(ProjectResponse updated)
    {
        var index = projects.FindIndex(project => project.Id == updated.Id);
        if (index >= 0) projects[index] = updated;
    }

    private void RequestDeleteProject(int id) =>
        pendingDeleteProject = projects.FirstOrDefault(project => project.Id == id);

    private void CancelDelete()
    {
        if (isDeleting) return;
        pendingDeleteProject = null;
    }

    private Task SetDeleteDialogOpen(bool open)
    {
        if (!open) CancelDelete();
        return Task.CompletedTask;
    }

    private async Task ConfirmDeleteProject()
    {
        if (pendingDeleteProject is null || isDeleting) return;

        var project = pendingDeleteProject;
        isDeleting = true;
        try
        {
            await ProjectsClient.DeleteAsync(project.Id, default);
            projects.RemoveAll(item => item.Id == project.Id);
            pendingDeleteProject = null;
            await ShowToast("Projeto excluído com sucesso!");
        }
        catch (Exception ex)
        {
            await ShowToast($"Não foi possível excluir: {ex.Message}");
        }
        finally
        {
            isDeleting = false;
        }
    }

    private async Task ExportCsv()
    {
        var csv = new StringBuilder("DC,Projeto,Cliente,Tipo,Status,Inicio,Fim,Dias Restantes,Renovacao,DM\n");
        foreach (var project in FilteredProjects)
        {
            var end = DateTime.TryParse(project.EndDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? (date.Date - DateTime.Today).Days.ToString(CultureInfo.InvariantCulture) : string.Empty;
            var values = new[] { project.Dc, project.Name, project.Client, project.Type, project.Status,
                project.StartDate, project.EndDate, end, project.Renewal, project.DeliveryManager };
            csv.AppendLine(string.Join(',', values.Select(CsvEscape)));
        }
        await JS.InvokeVoidAsync("downloadTextFile", "projetos.csv", csv.ToString());
        await ShowToast("CSV exportado com sucesso!");
    }

    private static string CsvEscape(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    private static string ProjectNames(IReadOnlyCollection<ProjectResponse> items, string emptyText) =>
        items.Count == 0 ? emptyText : string.Join(" · ", items.Take(2).Select(project => project.Name));

    private List<ProjectResponse> ProjectsWithin(
        Func<ProjectResponse, string?> dateSelector,
        DateTime start,
        DateTime end) =>
        projects.Where(project =>
            DateTime.TryParse(dateSelector(project), CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            && date.Date >= start
            && date.Date <= end).ToList();

    private static IEnumerable<ProjectResponse> CreatePreviewProjects() =>
    [
        new() { Id = 101, Dc = "DC1", Status = "Em andamento", Name = "Atlas Portal", Client = "Acme Corp", Type = "Squad", StartDate = "2026-03-10", EndDate = "2026-09-30", DeliveryManager = "Ana Prado", Renewal = "Em andamento", RenewalObservation = "Renovação em análise" },
        new() { Id = 102, Dc = "DC2", Status = "Programado", Name = "Orion Data Hub", Client = "Nexa Saúde", Type = "Escopo Fechado", StartDate = "2026-06-01", EndDate = "2026-08-15", DeliveryManager = "Bruno Lima", Renewal = "Aprovada", RenewalObservation = "Contrato aprovado" },
        new() { Id = 103, Dc = "DC3", Status = "Em andamento", Name = "Pulse Commerce", Client = "Vértice", Type = "Alocação", StartDate = "2026-01-20", EndDate = "2026-07-28", DeliveryManager = "Carla Mendes", Renewal = "Pendente", RenewalObservation = "Aguardando cliente" },
        new() { Id = 104, Dc = "DC1", Status = "Em andamento", Name = "Aurora Cloud", Client = "MobiPay", Type = "Squad", StartDate = "2025-11-03", EndDate = "2026-10-10", DeliveryManager = "Diego Alves", Renewal = "Em andamento", RenewalObservation = "Workshop agendado" },
        new() { Id = 105, Dc = "DC4", Status = "Encerrado", Name = "Nexus Insights", Client = "Grupo Lume", Type = "Escopo Fechado", StartDate = "2025-04-14", EndDate = "2026-05-22", DeliveryManager = "Eva Rocha", Renewal = "None" },
        new() { Id = 106, Dc = "DC2", Status = "Programado", Name = "Cobalto CX", Client = "Acme Corp", Type = "Squad", StartDate = "2026-07-01", EndDate = "2026-12-20", DeliveryManager = "Felipe Souza", Renewal = "Aprovada", RenewalObservation = "Renovação assinada" }
    ];
    private static string RenewalOptionLabel(string option) => option == "None" ? "Sem renovação" : option;
    private void HandleError(string message) => _ = ShowToast(message);

    private async Task ShowToast(string message)
    {
        toastCts?.Cancel();
        toastCts = new CancellationTokenSource();
        toastMessage = message;
        await InvokeAsync(StateHasChanged);
        try { await Task.Delay(3000, toastCts.Token); toastMessage = null; await InvokeAsync(StateHasChanged); }
        catch (TaskCanceledException) { }
    }

    public void Dispose()
    {
        disposed = true;
        NavigationManager.LocationChanged -= HandleLocationChanged;
        loadCancellation?.Cancel();
        toastCts?.Cancel();
    }
}
