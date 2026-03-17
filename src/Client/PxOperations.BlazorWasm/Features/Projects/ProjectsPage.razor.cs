using System.Globalization;
using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Projects;

public partial class ProjectsPage : ComponentBase
{
    private static readonly string[] deliveryCenters = ["DC1", "DC2", "DC3", "DC4", "DC5", "DC6"];
    private static readonly string[] statuses = ["Em andamento", "Programado", "Encerrado"];
    private static readonly string[] projectTypes = ["Squad", "Escopo Fechado", "Alocação"];

    [Inject] private ProjectsClient ProjectsClient { get; set; } = default!;

    // ── Data ─────────────────────────────────────────────────────────────────
    private bool isLoading = true;
    private string? errorMessage;
    private List<ProjectResponse> projects = [];

    // ── Filter / navigation state ─────────────────────────────────────────────
    private string searchTerm    = "";
    private string filterDc      = "";
    private string filterStatus  = "";
    private string filterType    = "";
    private string filterRenewal = "";
    private string activeTab     = "lista";

    // ── Modal state ───────────────────────────────────────────────────────────
    private bool showModal;
    private ProjectResponse? editingProject;

    // ── Toast ─────────────────────────────────────────────────────────────────
    private string? toastMessage;
    private CancellationTokenSource? toastCts;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    protected override async Task OnInitializedAsync()
    {
        try
        {
            var result = await ProjectsClient.ListAsync(null, null, null, null, null, default);
            projects = result.ToList();
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

    // ── Computed / stats ──────────────────────────────────────────────────────
    private List<ProjectResponse> FilteredProjects => projects
        .Where(p => string.IsNullOrEmpty(searchTerm)
            || p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            || (p.Client?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
        .Where(p => string.IsNullOrEmpty(filterDc)      || p.Dc     == filterDc)
        .Where(p => string.IsNullOrEmpty(filterStatus)  || p.Status == filterStatus)
        .Where(p => string.IsNullOrEmpty(filterType)    || p.Type   == filterType)
        .Where(p => string.IsNullOrEmpty(filterRenewal) || p.Renewal == filterRenewal)
        .ToList();

    private int ActiveCount           => projects.Count(p => p.Status == "Em andamento");
    private int ClientCount           => projects.Select(p => p.Client).Where(c => c is not null).Distinct().Count();
    private int ExpiringIn60DaysCount => projects.Count(p =>
    {
        if (p.EndDate is null) return false;
        if (!DateTime.TryParse(p.EndDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end)) return false;
        var days = (end.Date - DateTime.Today).Days;
        return days >= 0 && days <= 60;
    });
    private int RenewingCount         => projects.Count(p => p.Renewal == "Em andamento");
    private int ApprovedRenewalCount  => projects.Count(p => p.Renewal == "Aprovada");

    // ── Modal ─────────────────────────────────────────────────────────────────
    private void OpenCreateModal()
    {
        editingProject = null;
        showModal = true;
    }

    private void OpenEditModal(int id)
    {
        editingProject = projects.FirstOrDefault(p => p.Id == id);
        showModal = true;
    }

    private void CloseModal()
    {
        showModal = false;
        editingProject = null;
    }

    // ── Event handlers from child components ──────────────────────────────────
    private async Task HandleProjectSaved(ProjectResponse saved)
    {
        var idx = projects.FindIndex(p => p.Id == saved.Id);
        var verb = idx >= 0 ? "atualizado" : "criado";
        if (idx >= 0) projects[idx] = saved;
        else          projects.Add(saved);
        CloseModal();
        await ShowToast($"Projeto {verb} com sucesso!");
    }

    private void HandleProjectUpdated(ProjectResponse updated)
    {
        var idx = projects.FindIndex(p => p.Id == updated.Id);
        if (idx >= 0) projects[idx] = updated;
    }

    private async Task DeleteProject(int id)
    {
        try
        {
            await ProjectsClient.DeleteAsync(id, default);
            projects.RemoveAll(p => p.Id == id);
            await ShowToast("Projeto excluído com sucesso!");
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
    }

    private void HandleError(string message) => errorMessage = message;

    // ── Toast ─────────────────────────────────────────────────────────────────
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
        catch (TaskCanceledException) { }
    }
}
