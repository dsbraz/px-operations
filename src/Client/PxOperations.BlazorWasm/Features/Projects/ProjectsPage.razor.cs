using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Projects;

public partial class ProjectsPage : ComponentBase
{
    [Inject] private ProjectsClient ProjectsClient { get; set; } = default!;

    // ── List state ───────────────────────────────────────────────────────────
    private bool isLoading = true;
    private string? errorMessage;
    private List<ProjectResponse> projects = [];

    // ── Filter state (client-side) ───────────────────────────────────────────
    private string searchTerm = "";
    private string filterDc = "";
    private string filterStatus = "";
    private string filterType = "";
    private string filterRenewal = "";
    private string activeTab = "lista";

    // ── Modal state ──────────────────────────────────────────────────────────
    private bool showModal;
    private int? editingProjectId;
    private bool isSaving;
    private string? modalError;

    // ── Form fields ──────────────────────────────────────────────────────────
    private string formDc = "DC1";
    private string formStatus = "Em andamento";
    private string formName = "";
    private string? formClient;
    private string formType = "Squad";
    private DateTime? formStartDate;
    private DateTime? formEndDate;
    private string? formDeliveryManager;
    private string formRenewal = "None";
    private string? formRenewalObservation;

    // ── Toast ────────────────────────────────────────────────────────────────
    private string? toastMessage;
    private CancellationTokenSource? toastCts;

    // ── Lifecycle ────────────────────────────────────────────────────────────
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

    // ── Computed ─────────────────────────────────────────────────────────────
    private List<ProjectResponse> FilteredProjects => projects
        .Where(p => string.IsNullOrEmpty(searchTerm)
            || p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            || (p.Client?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
        .Where(p => string.IsNullOrEmpty(filterDc) || p.Dc == filterDc)
        .Where(p => string.IsNullOrEmpty(filterStatus) || p.Status == filterStatus)
        .Where(p => string.IsNullOrEmpty(filterType) || p.Type == filterType)
        .Where(p => string.IsNullOrEmpty(filterRenewal) || p.Renewal == filterRenewal)
        .ToList();

    private int ActiveCount => projects.Count(p => p.Status == "Em andamento");
    private int ClientCount => projects.Select(p => p.Client).Where(c => c is not null).Distinct().Count();
    private int ExpiringIn60DaysCount => projects.Count(p =>
    {
        if (p.EndDate is null) return false;
        if (!DateTime.TryParse(p.EndDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end)) return false;
        var days = (end.Date - DateTime.Today).Days;
        return days >= 0 && days <= 60;
    });
    private int RenewingCount => projects.Count(p => p.Renewal == "Em andamento");
    private int ApprovedRenewalCount => projects.Count(p => p.Renewal == "Aprovada");

    // ── Badge helpers ────────────────────────────────────────────────────────
    private static string GetStatusBadgeClass(string status) => status switch
    {
        "Em andamento" => "sb-and",
        "Programado"   => "sb-prog",
        _              => "sb-enc"
    };

    private static string GetStatusDot(string status) => status switch
    {
        "Em andamento" => "●",
        "Programado"   => "◌",
        _              => "○"
    };

    private static string GetTypeBadgeClass(string type) => type switch
    {
        "Squad"         => "tb-squad",
        "Escopo Fechado"=> "tb-escopo",
        _               => "tb-aloc"
    };

    private static string GetRenewalBadgeClass(string renewal) => renewal switch
    {
        "Aprovada"     => "rb-ap",
        "Em andamento" => "rb-and",
        "Pendente"     => "rb-pend",
        _              => "rb-na"
    };

    private static string GetRenewalIcon(string renewal) => renewal switch
    {
        "Aprovada"     => "✓",
        "Em andamento" => "↻",
        "Pendente"     => "⚑",
        _              => ""
    };

    private static Microsoft.AspNetCore.Components.MarkupString FormatDate(string? date)
    {
        if (date is null) return new("<span class=\"dtbd\">—</span>");
        if (!DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return new("<span class=\"dtbd\">—</span>");
        return new($"<span class=\"dval\">{d:dd/MM/yyyy}</span>");
    }

    private static Microsoft.AspNetCore.Components.MarkupString RenderRemainingDays(string? endDate)
    {
        if (endDate is null) return new("<span class=\"dpill dp-na\">—</span>");
        if (!DateTime.TryParse(endDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
            return new("<span class=\"dpill dp-na\">—</span>");
        var days = (end.Date - DateTime.Today).Days;
        if (days < 0) return new($"<span class=\"dpill dp-c\">{Math.Abs(days)}d atrás</span>");
        if (days <= 60) return new($"<span class=\"dpill dp-w\">{days}d</span>");
        return new($"<span class=\"dpill dp-ok\">{days}d</span>");
    }

    // ── Modal ────────────────────────────────────────────────────────────────
    private void OpenCreateModal()
    {
        editingProjectId = null;
        ResetForm();
        modalError = null;
        showModal = true;
    }

    private void OpenEditModal(int id)
    {
        var project = projects.FirstOrDefault(p => p.Id == id);
        if (project is null) return;

        editingProjectId = id;
        formDc = project.Dc;
        formStatus = project.Status;
        formName = project.Name;
        formClient = project.Client;
        formType = project.Type;
        formStartDate = project.StartDate is not null
            ? DateTime.Parse(project.StartDate, CultureInfo.InvariantCulture)
            : null;
        formEndDate = project.EndDate is not null
            ? DateTime.Parse(project.EndDate, CultureInfo.InvariantCulture)
            : null;
        formDeliveryManager = project.DeliveryManager;
        formRenewal = project.Renewal;
        formRenewalObservation = project.RenewalObservation;
        modalError = null;
        showModal = true;
    }

    private void CloseModal()
    {
        showModal = false;
        editingProjectId = null;
        modalError = null;
        ResetForm();
    }

    private void CloseModalOnOverlay() => CloseModal();

    private void ResetForm()
    {
        formDc = "DC1";
        formStatus = "Em andamento";
        formName = "";
        formClient = null;
        formType = "Squad";
        formStartDate = null;
        formEndDate = null;
        formDeliveryManager = null;
        formRenewal = "None";
        formRenewalObservation = null;
    }

    private async Task SaveProject()
    {
        if (string.IsNullOrWhiteSpace(formName))
        {
            modalError = "Informe o nome do projeto.";
            return;
        }

        isSaving = true;
        modalError = null;

        try
        {
            var startStr = formStartDate?.ToString("yyyy-MM-dd");
            var endStr = formEndDate?.ToString("yyyy-MM-dd");

            if (editingProjectId.HasValue)
            {
                var request = new UpdateProjectRequest
                {
                    Dc = formDc, Status = formStatus, Name = formName,
                    Client = formClient, Type = formType,
                    StartDate = startStr, EndDate = endStr,
                    DeliveryManager = formDeliveryManager,
                    Renewal = formRenewal, RenewalObservation = formRenewalObservation
                };
                var updated = await ProjectsClient.UpdateAsync(editingProjectId.Value, request, default);
                var idx = projects.FindIndex(p => p.Id == editingProjectId.Value);
                if (idx >= 0) projects[idx] = updated;
                CloseModal();
                await ShowToast("Projeto atualizado com sucesso!");
            }
            else
            {
                var request = new CreateProjectRequest
                {
                    Dc = formDc, Status = formStatus, Name = formName,
                    Client = formClient, Type = formType,
                    StartDate = startStr, EndDate = endStr,
                    DeliveryManager = formDeliveryManager,
                    Renewal = formRenewal, RenewalObservation = formRenewalObservation
                };
                var created = await ProjectsClient.CreateAsync(request, default);
                projects.Add(created);
                CloseModal();
                await ShowToast("Projeto criado com sucesso!");
            }
        }
        catch (Exception ex)
        {
            modalError = ex.Message;
        }
        finally
        {
            isSaving = false;
        }
    }

    // ── Delete ───────────────────────────────────────────────────────────────
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

    // ── Toast ────────────────────────────────────────────────────────────────
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
