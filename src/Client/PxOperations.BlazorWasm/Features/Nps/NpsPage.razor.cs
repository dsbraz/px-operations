using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps.Components;

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
    private string filterSearch = "";
    private bool includeDismissed;

    /// <summary>
    /// F1/D5: a subpágina vem da rota. Abrir /nps/resultados direto carrega a
    /// visão certa, e voltar/avançar do navegador transita entre elas.
    /// </summary>
    [Parameter] public string? Tab { get; set; }

    internal const string TabCollection = "coleta";
    internal const string TabResults = "resultados";
    internal const string TabResponses = "respostas";

    private static readonly IReadOnlyList<NpsTabs.NpsTab> TabDefinitions =
    [
        new(TabCollection, "Coleta"),
        new(TabResults, "Resultados"),
        new(TabResponses, "Respostas")
    ];

    private static readonly string[] DcOptions = ["DC1", "DC2", "DC3", "DC4", "DC5", "DC6"];
    private static readonly string[] ProjectTypeOptions = ["Squad", "Escopo Fechado", "Alocação"];

    // O protótipo abre em Coleta: é a tela de trabalho do operador.
    private string ActiveTab => Tab switch
    {
        TabResults => TabResults,
        TabResponses => TabResponses,
        _ => TabCollection
    };

    private string PortfolioCount => $"{projects.Count} projetos";

    private IReadOnlyList<NpsFilterBar.NpsFilterChip> FilterChips
    {
        get
        {
            var chips = new List<NpsFilterBar.NpsFilterChip>();
            if (!string.IsNullOrWhiteSpace(filterDc)) chips.Add(new("DC", filterDc, filterDc));
            if (!string.IsNullOrWhiteSpace(filterProjectType)) chips.Add(new("Tipo", filterProjectType, filterProjectType));
            if (includeDismissed) chips.Add(new("Dispensados", "Mostrando", "dismissed"));
            return chips;
        }
    }
    /// <summary>
    /// Passo 2 do F3: quando existe, o modal deixa de perguntar e passa a
    /// entregar — URL, validade em destaque e mensagem pronta para colar.
    /// </summary>
    private NpsDispatchDetailResponse? createdDispatch;

    private string dispatchFormat = "Simplificado";
    private string dispatchLanguage = "Português";

    private string ExportHref => BuildExportUrl();
    private bool CanCreateLink => selectedProjectId is not null;

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
    }

    private async Task OnSearchChanged(string value)
    {
        filterSearch = value;
        await RefreshAsync();
    }

    private async Task ToggleDc(string dc, ChangeEventArgs args)
    {
        filterDc = IsChecked(args) ? dc : string.Empty;
        await RefreshAsync();
    }

    private async Task ToggleProjectType(string type, ChangeEventArgs args)
    {
        filterProjectType = IsChecked(args) ? type : string.Empty;
        await RefreshAsync();
    }

    private async Task ToggleDismissed(ChangeEventArgs args)
    {
        includeDismissed = IsChecked(args);
        await RefreshAsync();
    }

    private async Task RemoveChip(NpsFilterBar.NpsFilterChip chip)
    {
        if (chip.Value == "dismissed") includeDismissed = false;
        else if (chip.Facet == "DC") filterDc = string.Empty;
        else if (chip.Facet == "Tipo") filterProjectType = string.Empty;

        await RefreshAsync();
    }

    private async Task ClearFilters()
    {
        filterDc = string.Empty;
        filterProjectType = string.Empty;
        includeDismissed = false;
        await RefreshAsync();
    }

    private static bool IsChecked(ChangeEventArgs args)
        => args.Value is true || (bool.TryParse(args.Value?.ToString(), out var parsed) && parsed);

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

    /// <summary>F6: reativar devolve o projeto à coluna que a regra indicar.</summary>
    private async Task ReactivateCollectionAsync(NpsProjectResponse project)
    {
        try
        {
            operationError = null;
            await NpsClient.ReactivateCollectionAsync(project.Id);
            await RefreshAsync();
        }
        catch (Exception)
        {
            operationError = "Não foi possível reativar a coleta.";
        }
    }

    private string CreateLinkSubtitle
    {
        get
        {
            if (createdDispatch is not null)
            {
                return $"{createdDispatch.Dispatch.ProjectName} · {createdDispatch.Dispatch.Format} · {createdDispatch.Dispatch.Language}";
            }

            return selectedDetail is null
                ? "Selecione o projeto que receberá o link"
                : $"{selectedDetail.Project.Name} · {selectedDetail.Project.Dc}";
        }
    }

    private string CreatedLinkUrl
        => createdDispatch is null ? string.Empty : BuildPublicFormUrl(createdDispatch.Targets.First().Token);

    /// <summary>F3: a validade vai em destaque, com a data por extenso.</summary>
    private string CreatedLinkExpiry
        => createdDispatch is not null && DateTimeOffset.TryParse(createdDispatch.Dispatch.ExpiresAt, out var expires)
            ? $"Este link vale por 20 dias: expira em {expires.ToLocalTime():dd/MM/yyyy}"
            : string.Empty;

    /// <summary>
    /// F3: mensagem pronta para colar, com o link e o prazo dentro. Montada no
    /// cliente — persistir template no backend é não-objetivo declarado.
    /// </summary>
    private string CreatedMessage
    {
        get
        {
            if (createdDispatch is null)
            {
                return string.Empty;
            }

            var project = createdDispatch.Dispatch.ProjectName;
            var url = CreatedLinkUrl;
            var prazo = DateTimeOffset.TryParse(createdDispatch.Dispatch.ExpiresAt, out var expires)
                ? expires.ToLocalTime().ToString("dd/MM")
                : "";

            if (string.Equals(createdDispatch.Dispatch.Language, "Inglês", StringComparison.OrdinalIgnoreCase))
            {
                return $"Hi! We're collecting NPS for the {project} project and your feedback really matters. "
                    + $"It takes under a minute and your answer is anonymous:\n{url}"
                    + $"\nThe survey is open until {prazo}. Thank you!";
            }

            return $"Olá! Estamos coletando o NPS do projeto {project} e a sua opinião faz diferença. "
                + $"Leva menos de 1 minuto e a resposta é anônima:\n{url}"
                + $"\nA pesquisa fica aberta até {prazo}. Obrigado!";
        }
    }

    private Task CopyCreatedLinkAsync() => CopyAsync(CreatedLinkUrl, "Não foi possível copiar o link.");

    private Task CopyCreatedMessageAsync() => CopyAsync(CreatedMessage, "Não foi possível copiar a mensagem.");

    private async Task CopyAsync(string text, string errorMessage)
    {
        operationError = null;
        try
        {
            await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
        catch (Exception)
        {
            // navigator.clipboard rejeita fora de contexto seguro; sem captura a
            // rejeição sobe pelo EventCallback e derruba a aplicação no WASM.
            operationError = errorMessage;
        }
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

            // O modal NÃO fecha: passa ao passo 2, que entrega a URL, a
            // validade e a mensagem. Fechar aqui esconderia o que o usuário
            // pediu para gerar.
            this.createdDispatch = createdDispatch;
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
        var search = string.IsNullOrWhiteSpace(filterSearch) ? null : filterSearch.Trim();

        dashboard = await NpsClient.GetDashboardAsync(search, dcFilter, null, projectTypeFilter, null, null, null, null);
        projects = (await NpsClient.ListProjectsAsync(search, dcFilter, null, projectTypeFilter, includeDismissed)).ToList();
    }

    private void OpenCreateLinkModal()
    {
        operationError = null;
        createdDispatch = null;
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
    {
        showCreateLinkModal = false;
        createdDispatch = null;
    }

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
        => response.BusinessValue is not null
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
