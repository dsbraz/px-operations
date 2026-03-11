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

    // ── Table collapse ───────────────────────────────────────────────────────
    private bool tableOpen = true;
    private void ToggleTable() => tableOpen = !tableOpen;

    // ── Weekly Pulse ─────────────────────────────────────────────────────────
    private bool pulseOpen = true;
    private void TogglePulse() => pulseOpen = !pulseOpen;

    private static (DateTime Monday, DateTime Sunday, DateTime PrevMonday, DateTime PrevSunday) GetWeekBounds()
    {
        var today = DateTime.Today;
        var dow = (int)today.DayOfWeek; // 0=Sun, 1=Mon, …
        var daysBack = dow == 0 ? 6 : dow - 1;
        var monday = today.AddDays(-daysBack);
        var sunday = monday.AddDays(6);
        return (monday, sunday, monday.AddDays(-7), sunday.AddDays(-7));
    }

    private string WeekLabel
    {
        get
        {
            var (mon, sun, _, _) = GetWeekBounds();
            return $"{mon:dd/MM} – {sun:dd/MM}";
        }
    }

    private List<ProjectResponse> PulseNewScheduled =>
        projects.Where(p => p.Status == "Programado").ToList();

    private List<ProjectResponse> PulseStartedLastWeek
    {
        get
        {
            var (_, _, prevMon, prevSun) = GetWeekBounds();
            return projects.Where(p =>
            {
                if (p.StartDate is null) return false;
                if (!DateTime.TryParse(p.StartDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return false;
                return d.Date >= prevMon.Date && d.Date <= prevSun.Date;
            }).ToList();
        }
    }

    private List<ProjectResponse> PulseEndedLastWeek
    {
        get
        {
            var (_, _, prevMon, prevSun) = GetWeekBounds();
            return projects.Where(p =>
            {
                if (p.EndDate is null) return false;
                if (!DateTime.TryParse(p.EndDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return false;
                return d.Date >= prevMon.Date && d.Date <= prevSun.Date;
            }).ToList();
        }
    }

    private List<ProjectResponse> PulseRenewalApproved =>
        projects.Where(p => p.Renewal == "Aprovada").ToList();

    // ── Kanban view ───────────────────────────────────────────────────────────
    private string kanbanGroupBy = "status";

    private List<(string Key, string Label, string ColorClass, List<ProjectResponse> Items)> KanbanColumns
    {
        get
        {
            var filtered = FilteredProjects;
            List<(string Key, string Label, string ColorClass, List<ProjectResponse> Items)> cols;

            if (kanbanGroupBy == "renewal")
            {
                cols =
                [
                    ("None",         "Sem status",   "kb-gray",   filtered.Where(p => p.Renewal is "None" or null or "").ToList()),
                    ("Pendente",     "Pendente",     "kb-orange",  filtered.Where(p => p.Renewal == "Pendente").ToList()),
                    ("Em andamento", "Em andamento", "kb-blue",    filtered.Where(p => p.Renewal == "Em andamento").ToList()),
                    ("Aprovada",     "Aprovada",     "kb-green",   filtered.Where(p => p.Renewal == "Aprovada").ToList()),
                ];
                return cols;
            }

            if (kanbanGroupBy == "dc")
                return new[] { "DC1", "DC2", "DC3", "DC4", "DC5", "DC6" }
                    .Select(dc => (Key: dc, Label: dc, ColorClass: "kb-purple",
                                   Items: filtered.Where(p => p.Dc == dc).ToList()))
                    .ToList();

            // default: "status"
            cols =
            [
                ("Programado",   "Programado",   "kb-orange", filtered.Where(p => p.Status == "Programado").ToList()),
                ("Em andamento", "Em andamento", "kb-green",  filtered.Where(p => p.Status == "Em andamento").ToList()),
                ("Encerrado",    "Encerrado",    "kb-gray",   filtered.Where(p => p.Status == "Encerrado").ToList()),
            ];
            return cols;
        }
    }

    // ── Drag & drop ───────────────────────────────────────────────────────────
    private ProjectResponse? draggedProject;
    private string? dragOverColumnKey;

    private void OnDragStart(ProjectResponse project)
    {
        draggedProject = project;
        dragOverColumnKey = null;
    }

    private void OnDragEnter(string columnKey) => dragOverColumnKey = columnKey;

    private void OnDragLeave(string columnKey)
    {
        if (dragOverColumnKey == columnKey)
            dragOverColumnKey = null;
    }

    private void OnDragEnd()
    {
        draggedProject = null;
        dragOverColumnKey = null;
    }

    private async Task OnDrop(string columnKey)
    {
        if (draggedProject is null) return;
        if (GetCurrentKey(draggedProject) == columnKey) { OnDragEnd(); return; }

        var request = new UpdateProjectRequest
        {
            Dc = draggedProject.Dc, Status = draggedProject.Status,
            Name = draggedProject.Name, Client = draggedProject.Client,
            Type = draggedProject.Type, StartDate = draggedProject.StartDate,
            EndDate = draggedProject.EndDate, DeliveryManager = draggedProject.DeliveryManager,
            Renewal = draggedProject.Renewal, RenewalObservation = draggedProject.RenewalObservation,
        };

        switch (kanbanGroupBy)
        {
            case "status":  request.Status  = columnKey; break;
            case "renewal": request.Renewal = columnKey; break;
            case "dc":      request.Dc      = columnKey; break;
        }

        try
        {
            var updated = await ProjectsClient.UpdateAsync(draggedProject.Id, request, default);
            var idx = projects.FindIndex(p => p.Id == updated.Id);
            if (idx >= 0) projects[idx] = updated;
            await ShowToast("Projeto movido com sucesso!");
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            OnDragEnd();
        }
    }

    private string GetCurrentKey(ProjectResponse p) => kanbanGroupBy switch
    {
        "renewal" => p.Renewal is null or "" ? "None" : p.Renewal,
        "dc"      => p.Dc,
        _         => p.Status,
    };

    // ── Renovações view ───────────────────────────────────────────────────────
    private string renovYear = "2026";
    private string renovPeriod = "ano";
    private string renovDc = "";

    private bool InRenovPeriod(string? endDate)
    {
        if (endDate is null) return false;
        if (!DateTime.TryParse(endDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return false;
        if (!int.TryParse(renovYear, out var year) || d.Year != year) return false;
        return renovPeriod switch
        {
            "q1" => d.Month <= 3,
            "q2" => d.Month >= 4 && d.Month <= 6,
            "q3" => d.Month >= 7 && d.Month <= 9,
            "q4" => d.Month >= 10,
            _    => true // "ano"
        };
    }

    private List<ProjectResponse> RenovScope => projects
        .Where(p => p.Status == "Em andamento")
        .Where(p => string.IsNullOrEmpty(renovDc) || p.Dc == renovDc)
        .Where(p => InRenovPeriod(p.EndDate))
        .ToList();

    private int RenovTotal       => RenovScope.Count;
    private int RenovApproved    => RenovScope.Count(p => p.Renewal == "Aprovada");
    private int RenovInProgress  => RenovScope.Count(p => p.Renewal == "Em andamento");
    private int RenovPending     => RenovScope.Count(p => p.Renewal == "Pendente");
    private int RenovNoStatus    => RenovTotal - RenovApproved - RenovInProgress - RenovPending;
    private int RenovCoveragePct => RenovTotal == 0 ? 0
        : (RenovApproved + RenovInProgress + RenovPending) * 100 / RenovTotal;

    private string RenovPeriodLabel => renovPeriod switch
    {
        "q1" => "Q1", "q2" => "Q2", "q3" => "Q3", "q4" => "Q4",
        _    => "Ano completo"
    };

    private List<(string Dc, int Total, int WithStatus, int Approved, int Pct)> RenovDcBars
    {
        get
        {
            var dcs = projects.Select(p => p.Dc).Distinct().OrderBy(d => d).ToList();
            return dcs.Select(dc =>
            {
                var scoped = projects
                    .Where(p => p.Status == "Em andamento" && p.Dc == dc && InRenovPeriod(p.EndDate))
                    .ToList();
                var total      = scoped.Count;
                var withStatus = scoped.Count(p => p.Renewal is not ("None" or null or ""));
                var approved   = scoped.Count(p => p.Renewal == "Aprovada");
                var pct        = total == 0 ? 0 : withStatus * 100 / total;
                return (dc, total, withStatus, approved, pct);
            }).ToList();
        }
    }

    private List<ProjectResponse> RenovCards =>
        RenovScope.Where(p => p.Renewal is not ("None" or null or "")).ToList();

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
