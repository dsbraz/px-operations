using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Nps;

public partial class NpsPage : ComponentBase
{
    [Inject] private NpsClient NpsClient { get; set; } = default!;
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    private NpsDashboardResponse? dashboard;
    private List<NpsProjectResponse> projects = [];
    private NpsProjectDetailResponse? selectedDetail;
    private NpsDispatchDetailResponse? selectedDispatchDetail;
    private List<NpsSurveyResponse> responses = [];
    private bool isLoading = true;
    private string? loadError;
    private string? operationError;
    private int? selectedProjectId;
    private bool showCreateLinkModal;
    private bool showDetailModal;

    private string filterDc = "";
    private string filterProjectType = "";
    private string dispatchFormat = "Simplificado";
    private string dispatchLanguage = "Português";

    private string ExportHref => BuildExportUrl();
    private bool CanCreateLink => selectedProjectId is not null;

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
    }

    private async Task OnFilterDcChanged(ChangeEventArgs args)
    {
        filterDc = args.Value?.ToString() ?? string.Empty;
        await RefreshAsync();
    }

    private async Task OnFilterProjectTypeChanged(ChangeEventArgs args)
    {
        filterProjectType = args.Value?.ToString() ?? string.Empty;
        await RefreshAsync();
    }

    private async Task OnCreateLinkProjectChanged(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), out var projectId))
        {
            selectedProjectId = null;
            selectedDetail = null;
            responses = [];
            selectedDispatchDetail = null;
            return;
        }

        await SelectProjectAsync(projectId);
    }

    private async Task RefreshAsync()
    {
        isLoading = true;
        loadError = null;
        operationError = null;

        try
        {
            await LoadDashboardAndProjectsAsync();

            if (selectedProjectId.HasValue && projects.Any(p => p.Id == selectedProjectId.Value))
            {
                await SelectProjectAsync(selectedProjectId.Value);
            }
            else
            {
                selectedProjectId = null;
                selectedDetail = null;
                responses = [];
                showCreateLinkModal = false;
                showDetailModal = false;
            }
        }
        catch (Exception)
        {
            loadError = "Não foi possível carregar o módulo NPS.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SelectProjectAsync(int projectId)
    {
        selectedProjectId = projectId;
        selectedDetail = await NpsClient.GetProjectAsync(projectId);
        responses = selectedDetail?.RecentResponses.ToList() ?? [];
        selectedDispatchDetail = null;
    }

    private async Task OpenProjectDetailAsync(int projectId)
    {
        await SelectProjectAsync(projectId);
        showDetailModal = true;
    }

    private async Task CreateDispatchAsync()
    {
        if (selectedProjectId is null)
        {
            return;
        }

        try
        {
            operationError = null;
            var periodStart = DateOnly.FromDateTime(DateTime.Today);
            var periodEnd = periodStart.AddDays(90);
            var createdDispatch = await NpsClient.CreateDispatchAsync(new CreateNpsDispatchRequest
            {
                ProjectId = selectedProjectId.Value,
                PeriodStart = periodStart.ToString("yyyy-MM-dd"),
                PeriodEnd = periodEnd.ToString("yyyy-MM-dd"),
                Format = dispatchFormat,
                Language = dispatchLanguage,
                CreatedBy = "Operations PX",
                ContactIds = [],
                CreateGenericToken = true
            });

            await SelectProjectAsync(selectedProjectId.Value);
            selectedDispatchDetail = createdDispatch;
            responses = [];
            showCreateLinkModal = false;
            showDetailModal = true;
            await LoadDashboardAndProjectsAsync();
        }
        catch (Exception)
        {
            operationError = "Não foi possível criar o disparo.";
        }
    }

    private async Task SelectDispatchAsync(int dispatchId)
    {
        selectedDispatchDetail = await NpsClient.GetDispatchAsync(dispatchId);
        responses = (await NpsClient.ListResponsesAsync(dispatchId)).ToList();
    }

    private async Task LoadDashboardAndProjectsAsync()
    {
        var dcFilter = string.IsNullOrWhiteSpace(filterDc) ? null : filterDc;
        var projectTypeFilter = string.IsNullOrWhiteSpace(filterProjectType) ? null : filterProjectType;

        dashboard = await NpsClient.GetDashboardAsync(null, dcFilter, null, projectTypeFilter, null, null, null, null);
        projects = (await NpsClient.ListProjectsAsync(null, dcFilter, null, projectTypeFilter)).ToList();
    }

    private void OpenCreateLinkModal()
    {
        operationError = null;
        showCreateLinkModal = true;
    }

    private async Task OpenCreateLinkModal(int projectId)
    {
        if (selectedProjectId != projectId || selectedDetail is null)
        {
            await SelectProjectAsync(projectId);
        }

        OpenCreateLinkModal();
    }

    private void CloseCreateLinkModal()
        => showCreateLinkModal = false;

    private void CloseDetailModal()
        => showDetailModal = false;

    private string BuildExportUrl()
    {
        var query = new Dictionary<string, string?>
        {
            ["dc"] = string.IsNullOrWhiteSpace(filterDc) ? null : filterDc,
            ["projectType"] = string.IsNullOrWhiteSpace(filterProjectType) ? null : filterProjectType,
            ["projectId"] = selectedProjectId?.ToString()
        };

        var values = query
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value!)}");
        var queryString = string.Join('&', values);
        var relativeUrl = string.IsNullOrEmpty(queryString)
            ? "api/nps/responses/export"
            : $"api/nps/responses/export?{queryString}";

        return HttpClient.BaseAddress is null
            ? relativeUrl
            : new Uri(HttpClient.BaseAddress, relativeUrl).ToString();
    }

    private string BuildPublicFormUrl(Guid token)
        => NavigationManager.ToAbsoluteUri($"nps/{token}").ToString();

    private async Task CopyPublicFormUrlAsync(Guid token)
    {
        operationError = null;
        await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", BuildPublicFormUrl(token));
    }

    private static string FormatTimestamp(string value)
        => DateTimeOffset.TryParse(value, out var timestamp)
            ? timestamp.ToString("yyyy-MM-dd HH:mm")
            : value;

    private static string ResponseIdentity(NpsSurveyResponse response)
        => response.RespondentEmail
            ?? response.RespondentName
            ?? response.ContactEmail
            ?? response.ContactName
            ?? "";

    private static string ResponseIdentityOrFallback(NpsSurveyResponse response)
        => string.IsNullOrWhiteSpace(ResponseIdentity(response))
            ? "Respondente não identificado"
            : ResponseIdentity(response);

    private static bool HasDimensionAnswers(NpsSurveyResponse response)
        => response.Scope is not null
            || response.Schedule is not null
            || response.Quality is not null
            || response.Communication is not null;

    private static string ScorePercent(int? score)
        => $"{Math.Clamp(score.GetValueOrDefault(), 0, 10) * 10}%";

    private static string ClassificationClass(NpsSurveyResponse response)
        => response.Classification switch
        {
            "Promotor" => "promoter",
            "Neutro" => "passive",
            "Detrator" => "detractor",
            _ => "passive"
        };

    private static string ProjectStatusLabel(NpsProjectResponse project)
    {
        if (project.LastResponseAt is not null)
        {
            return "Respondido";
        }

        if (project.ActiveDispatches > 0)
        {
            return "Link gerado";
        }

        return project.IsOverdue ? "Pendente" : "Sem link";
    }

    private static string ProjectStatusClass(NpsProjectResponse project)
        => ProjectStatusLabel(project) switch
        {
            "Respondido" => "ok",
            "Link gerado" => "info",
            _ => "late"
        };

    private static string LinkStatusLabel(NpsProjectResponse project)
    {
        if (project.LinkTargetsCount == 0)
        {
            return "Sem link";
        }

        return project.AnsweredLinkTargetsCount >= project.LinkTargetsCount ? "Respondido" : "Aberto";
    }

    private static string LinkStatusClass(NpsProjectResponse project)
        => LinkStatusLabel(project) switch
        {
            "Respondido" => "ok",
            "Aberto" => "info",
            _ => "late"
        };

    private static string LinkStatusLabel(NpsDispatchResponse dispatch)
    {
        if (dispatch.TargetsCount == 0)
        {
            return "Sem link";
        }

        return dispatch.ResponsesCount >= dispatch.TargetsCount ? "Respondido" : "Aberto";
    }

    private static string LinkStatusClass(NpsDispatchResponse dispatch)
        => LinkStatusLabel(dispatch) switch
        {
            "Respondido" => "ok",
            "Aberto" => "info",
            _ => "late"
        };

    private static string LastResponseLabel(NpsProjectResponse project)
        => project.LastResponseAt is null ? "Sem resposta" : FormatTimestamp(project.LastResponseAt);

    private static string TargetLabel(NpsDispatchTargetResponse target)
        => target.IsGeneric ? "Link de resposta" : target.ContactName ?? target.ContactEmail ?? "Contato";
}
