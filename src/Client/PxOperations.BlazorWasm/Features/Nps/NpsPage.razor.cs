using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PxOperations.BlazorWasm.Api;
using PxOperations.Ui.Components.DataDisplay;

namespace PxOperations.BlazorWasm.Features.Nps;

public partial class NpsPage : ComponentBase, IDisposable
{
    [Inject] private NpsClient NpsClient { get; set; } = default!;
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    private readonly HashSet<string> clients = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> dcs = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> projectTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> deliveryManagers = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> statuses = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> formats = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> classifications = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? loadCancellation;
    private CancellationTokenSource? searchCancellation;
    private CancellationTokenSource? expansionCancellation;
    private int loadVersion;
    private bool redirecting;
    private bool isLoading;
    private string? loadError;
    private string search = string.Empty;
    private string? from;
    private string? to;
    private bool includeWaived;
    private NpsDashboardView? dashboard;
    private NpsFilterOptionsView? filterOptions;
    private List<NpsProjectView> projects = [];
    private List<NpsProjectResultView> projectResults = [];
    private string resultSort = "project";
    private bool resultSortDescending;
    private int? expandedProjectId;
    private List<NpsResponseView> expandedResponses = [];
    private bool expansionLoading;
    private string? expansionError;
    private List<NpsResponseView> responseAudit = [];
    private bool responseDialogOpen;
    private NpsResponseView? selectedResponse;

    private bool createDialogOpen;
    private int createProjectId;
    private string createFormat = "complete";
    private string createLanguage = "pt";
    private NpsDispatchDetailView? createdDispatch;
    private bool detailDialogOpen;
    private NpsProjectDetailView? selectedDetail;
    private List<NpsResponseView> detailResponses = [];
    private string detailFormat = "all";
    private bool waiverDialogOpen;
    private int waiverProjectId;
    private string waiverReason = string.Empty;

    private static readonly IReadOnlyList<BoardColumn> BoardColumns =
    [
        new("no_link", "Sem link", "kb-gray"),
        new("awaiting_response", "Aguardando resposta", "kb-orange"),
        new("recollection", "Recoleta", "kb-purple"),
        new("current", "Em dia", "kb-green")
    ];

    private NpsTab ActiveTab { get; set; }
    private bool ShowsFilters => true;
    private bool ShowsIndicators => ActiveTab is NpsTab.Collection or NpsTab.Results;
    private NpsFilterOptionsView FilterOptions => filterOptions ?? dashboard?.FilterOptions ?? new NpsFilterOptionsView();
    private IReadOnlyList<NpsProjectView> WaivedProjects => projects.Where(project => project.Stage.Code == "waived").ToArray();
    private string CreateDialogDescription => createdDispatch is null
        ? "Escolha a rodada que será compartilhada."
        : "O link usa a validade devolvida pelo servidor.";
    private string ExportHref => new Uri(HttpClient.BaseAddress!, $"api/nps/responses/export{BuildResponseQuery()}").ToString();
    private int ActiveFacetCount => ActiveChips.Count;

    private IReadOnlyList<FilterChip> ActiveChips
    {
        get
        {
            var chips = new List<FilterChip>();
            AddChip(chips, "client", "Cliente", clients);
            AddChip(chips, "dc", "DC", dcs);
            AddChip(chips, "projectType", "Tipo", projectTypes);
            AddChip(chips, "deliveryManager", "DM", deliveryManagers);
            if (ActiveTab == NpsTab.Results)
            {
                AddChip(chips, "status", "Status", statuses);
                if (from is not null || to is not null)
                {
                    chips.Add(new FilterChip("period", "Período", $"{from ?? "…"} a {to ?? "…"}"));
                }
            }
            else if (ActiveTab == NpsTab.Responses)
            {
                AddChip(chips, "format", "Formato", formats);
                AddChip(chips, "classification", "Classificação", classifications);
                if (from is not null || to is not null)
                {
                    chips.Add(new FilterChip("period", "Período", $"{from ?? "…"} a {to ?? "…"}"));
                }
            }
            else if (ActiveTab == NpsTab.Collection && includeWaived)
            {
                chips.Add(new FilterChip("includeWaived", "Dispensados", "Mostrar"));
            }

            return chips;
        }
    }

    private string SuggestedMessage
    {
        get
        {
            if (createdDispatch is null)
            {
                return string.Empty;
            }

            var target = createdDispatch.Targets.Single(item => item.IsGeneric);
            var project = projects.FirstOrDefault(item => item.Id == createdDispatch.Dispatch.ProjectId)?.Name
                ?? createdDispatch.Dispatch.ProjectName;
            var expires = createdDispatch.Dispatch.ExpiresAt.ToString("dd/MM/yyyy");
            var url = PublicUrl(target.Token);
            return createLanguage switch
            {
                "en" => $"We would like your feedback about {project}. Please respond by {expires}: {url}",
                "es" => $"Nos gustaría recibir tu opinión sobre {project}. Responde hasta el {expires}: {url}",
                _ => $"Gostaríamos da sua opinião sobre {project}. Responda até {expires}: {url}"
            };
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        var path = NavigationManager.ToAbsoluteUri(NavigationManager.Uri).AbsolutePath.TrimEnd('/');
        if (path == "/nps")
        {
            redirecting = true;
            NavigationManager.NavigateTo($"/nps/coleta{CurrentRawQuery()}", replace: true);
            return;
        }

        redirecting = false;
        var nextTab = path.EndsWith("/resultados", StringComparison.Ordinal)
            ? NpsTab.Results
            : path.EndsWith("/respostas", StringComparison.Ordinal)
                ? NpsTab.Responses
                : NpsTab.Collection;
        if (nextTab != ActiveTab)
        {
            CloseResultExpansion();
        }

        ActiveTab = nextTab;
        ReadQuery();
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (redirecting)
        {
            return;
        }

        loadCancellation?.Cancel();
        loadCancellation?.Dispose();
        loadCancellation = new CancellationTokenSource();
        var ct = loadCancellation.Token;
        var version = ++loadVersion;
        isLoading = true;
        loadError = null;

        try
        {
            if (ActiveTab == NpsTab.Results)
            {
                var dashboardTask = LoadDashboardAsync(ct);
                var resultsTask = LoadProjectResultsAsync(ct);
                await Task.WhenAll(dashboardTask, resultsTask);
                if (version == loadVersion)
                {
                    dashboard = dashboardTask.Result;
                    filterOptions = dashboardTask.Result.FilterOptions;
                    projectResults = resultsTask.Result.ToList();
                }
            }
            else if (ActiveTab == NpsTab.Responses)
            {
                var optionsTask = filterOptions is null
                    ? NpsClient.GetFilterOptionsAsync(ct)
                    : Task.FromResult(filterOptions);
                var responsesTask = LoadResponsesAsync(ct);
                await Task.WhenAll(optionsTask, responsesTask);
                if (version == loadVersion)
                {
                    filterOptions = optionsTask.Result;
                    responseAudit = responsesTask.Result
                        .OrderByDescending(response => response.SubmittedAt)
                        .ThenByDescending(response => response.Id)
                        .ToList();
                }
            }
            else
            {
                var dashboardTask = LoadDashboardAsync(ct);
                var projectsTask = LoadProjectsAsync(ct);
                await Task.WhenAll(dashboardTask, projectsTask);
                if (version == loadVersion)
                {
                    dashboard = dashboardTask.Result;
                    filterOptions = dashboardTask.Result.FilterOptions;
                    projects = projectsTask.Result.ToList();
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (version == loadVersion)
            {
                loadError = "Não foi possível carregar o módulo NPS.";
                dashboard = null;
                projects = [];
                projectResults = [];
                responseAudit = [];
            }
        }
        finally
        {
            if (version == loadVersion)
            {
                isLoading = false;
            }
        }
    }

    private Task<NpsDashboardView> LoadDashboardAsync(CancellationToken ct)
        => NpsClient.GetDashboardAsync(
            NullIfEmpty(search), clients, dcs, projectTypes, deliveryManagers,
            ActiveTab == NpsTab.Results ? statuses : [],
            [],
            [],
            ActiveTab == NpsTab.Results ? from : null,
            ActiveTab == NpsTab.Results ? to : null,
            includeWaived,
            null,
            ct);

    private Task<ICollection<NpsProjectView>> LoadProjectsAsync(CancellationToken ct)
        => NpsClient.ListProjectsAsync(
            NullIfEmpty(search), clients, dcs, projectTypes, deliveryManagers,
            [], [], [], null, null, includeWaived, null, ct);

    private Task<ICollection<NpsProjectResultView>> LoadProjectResultsAsync(CancellationToken ct)
        => NpsClient.ListProjectResultsAsync(
            NullIfEmpty(search), clients, dcs, projectTypes, deliveryManagers,
            statuses, [], [], from, to, false, null, ct);

    private Task<ICollection<NpsResponseView>> LoadResponsesAsync(CancellationToken ct)
        => NpsClient.ListResponsesAsync(
            NullIfEmpty(search), clients, dcs, projectTypes, deliveryManagers,
            [], formats, classifications, from, to, null, null, ct);

    private async Task SearchChanged(ChangeEventArgs args)
    {
        search = args.Value?.ToString() ?? string.Empty;
        searchCancellation?.Cancel();
        searchCancellation?.Dispose();
        searchCancellation = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, searchCancellation.Token);
            await UpdateUrlAndReloadAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ToggleFacetAsync(HashSet<string> facet, string value)
    {
        if (!facet.Add(value))
        {
            facet.Remove(value);
        }

        await UpdateUrlAndReloadAsync();
    }

    private async Task ToggleWaivedAsync()
    {
        includeWaived = !includeWaived;
        await UpdateUrlAndReloadAsync();
    }

    private async Task FromChangedAsync(ChangeEventArgs args)
    {
        from = NullIfEmpty(args.Value?.ToString());
        await UpdateUrlAndReloadAsync();
    }

    private async Task ToChangedAsync(ChangeEventArgs args)
    {
        to = NullIfEmpty(args.Value?.ToString());
        await UpdateUrlAndReloadAsync();
    }

    private async Task ClearFiltersAsync()
    {
        CloseResultExpansion();
        search = string.Empty;
        clients.Clear();
        dcs.Clear();
        projectTypes.Clear();
        deliveryManagers.Clear();
        statuses.Clear();
        formats.Clear();
        classifications.Clear();
        from = null;
        to = null;
        includeWaived = false;
        NavigationManager.NavigateTo($"/nps/{TabCode(ActiveTab)}", replace: true);
        await ReloadAsync();
    }

    private async Task RemoveFacetAsync(string key)
    {
        switch (key)
        {
            case "client": clients.Clear(); break;
            case "dc": dcs.Clear(); break;
            case "projectType": projectTypes.Clear(); break;
            case "deliveryManager": deliveryManagers.Clear(); break;
            case "status": statuses.Clear(); break;
            case "format": formats.Clear(); break;
            case "classification": classifications.Clear(); break;
            case "period": from = null; to = null; break;
            case "includeWaived": includeWaived = false; break;
        }

        await UpdateUrlAndReloadAsync();
    }

    private async Task UpdateUrlAndReloadAsync()
    {
        CloseResultExpansion();
        NavigationManager.NavigateTo($"/nps/{TabCode(ActiveTab)}{BuildQuery()}", replace: true);
        await ReloadAsync();
    }

    private async Task OpenCreateDialog()
    {
        createdDispatch = null;
        createProjectId = 0;
        createFormat = "complete";
        createLanguage = "pt";
        if (projects.Count == 0)
        {
            try
            {
                projects = (await LoadProjectsAsync(CancellationToken.None)).ToList();
            }
            catch (Exception)
            {
                loadError = "Não foi possível carregar os projetos para gerar o link.";
                return;
            }
        }

        createDialogOpen = true;
    }

    private async Task CreateDispatchAsync()
    {
        if (createProjectId == 0)
        {
            return;
        }

        createdDispatch = await NpsClient.CreateDispatchAsync(new CreateNpsDispatchRequest
        {
            ProjectId = createProjectId,
            Format = createFormat,
            Language = createLanguage,
            ContactIds = []
        });
    }

    private async Task RunPrimaryActionAsync(NpsProjectView project)
    {
        var action = project.PrimaryAction;
        if (action is null)
        {
            return;
        }

        if (action.Code == "reactivate")
        {
            await ReactivateAsync(project.Id);
            return;
        }

        if (action.Code == "copy_link" && action.Token.HasValue)
        {
            await CopyAsync(PublicUrl(action.Token.Value));
            return;
        }

        createdDispatch = null;
        createProjectId = project.Id;
        createFormat = action.Format ?? "complete";
        createLanguage = "pt";
        createDialogOpen = true;
    }

    private async Task OpenDetailAsync(int projectId)
    {
        selectedDetail = await NpsClient.GetProjectAsync(projectId);
        detailResponses = selectedDetail.RecentResponses.ToList();
        detailFormat = "all";
        detailDialogOpen = true;
    }

    private async Task FilterDetailResponsesAsync(string format, IReadOnlyList<string> selectedFormats)
    {
        if (selectedDetail is null)
        {
            return;
        }

        detailFormat = format;
        detailResponses = (await NpsClient.ListProjectResponsesAsync(
            selectedDetail.Project.Id,
            null, [], [], [], [], [], selectedFormats, [], null, null, null, null)).ToList();
    }

    private Task FilterAllDetailResponsesAsync() => FilterDetailResponsesAsync("all", []);
    private Task FilterCompleteDetailResponsesAsync() => FilterDetailResponsesAsync("complete", ["complete"]);
    private Task FilterSimplifiedDetailResponsesAsync() => FilterDetailResponsesAsync("simplified", ["simplified"]);

    private void OpenWaiverDialog(int projectId)
    {
        waiverProjectId = projectId;
        waiverReason = string.Empty;
        waiverDialogOpen = true;
    }

    private void CloseCreateDialog() => createDialogOpen = false;
    private void CloseDetailDialog() => detailDialogOpen = false;
    private void CloseWaiverDialog() => waiverDialogOpen = false;

    private async Task WaiveAsync()
    {
        await NpsClient.WaiveCollectionAsync(waiverProjectId, new WaiveNpsCollectionRequest { Reason = waiverReason });
        waiverDialogOpen = false;
        await ReloadAsync();
    }

    private async Task ReactivateAsync(int projectId)
    {
        await NpsClient.ReactivateCollectionAsync(projectId);
        await ReloadAsync();
    }

    private IReadOnlyList<NpsProjectView> ProjectsForStage(string stage)
        => projects.Where(project => project.Stage.Code == stage).ToArray();

    private IEnumerable<NpsProjectResultView> SortedProjectResults => resultSort switch
    {
        "client" => OrderProjectResults(result => result.Client ?? string.Empty),
        "dc" => OrderProjectResults(result => result.Dc),
        "responses" => OrderProjectResults(result => result.ResponsesCount),
        "nps" => OrderProjectResults(result => result.OfficialNps ?? double.MinValue),
        _ => OrderProjectResults(result => result.Name)
    };

    private IEnumerable<NpsProjectResultView> OrderProjectResults<TKey>(Func<NpsProjectResultView, TKey> key)
        => resultSortDescending
            ? projectResults.OrderByDescending(key).ThenBy(result => result.Name, StringComparer.CurrentCultureIgnoreCase)
            : projectResults.OrderBy(key).ThenBy(result => result.Name, StringComparer.CurrentCultureIgnoreCase);

    private void SortResults(string column)
    {
        if (resultSort == column)
        {
            resultSortDescending = !resultSortDescending;
        }
        else
        {
            resultSort = column;
            resultSortDescending = false;
        }

        CloseResultExpansion();
    }

    private string AriaSort(string column)
        => resultSort != column ? "none" : resultSortDescending ? "descending" : "ascending";

    private async Task ToggleResultExpansionAsync(NpsProjectResultView result)
    {
        if (expandedProjectId == result.Id)
        {
            CloseResultExpansion();
            return;
        }

        expansionCancellation?.Cancel();
        expansionCancellation?.Dispose();
        var expansion = new CancellationTokenSource();
        expansionCancellation = expansion;
        expandedProjectId = result.Id;
        expandedResponses = [];
        expansionError = null;
        expansionLoading = true;

        try
        {
            expandedResponses = (await NpsClient.ListProjectResponsesAsync(
                result.Id,
                null, [], [], [], [], [], [], [], from, to, null, null,
                expansion.Token)).ToList();
        }
        catch (OperationCanceledException) when (expansion.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            if (expandedProjectId == result.Id)
            {
                expansionError = "Não foi possível carregar as respostas deste projeto.";
            }
        }
        finally
        {
            if (expandedProjectId == result.Id)
            {
                expansionLoading = false;
            }
        }
    }

    private Task RetryResultExpansionAsync(NpsProjectResultView result)
    {
        expandedProjectId = null;
        return ToggleResultExpansionAsync(result);
    }

    private IEnumerable<IGrouping<string, NpsResponseView>> GroupedExpandedResponses
        => expandedResponses
            .OrderBy(response => ClassificationOrder(response.Classification))
            .ThenByDescending(response => response.SubmittedAt)
            .GroupBy(response => response.Classification);

    private static int ClassificationOrder(string classification) => classification switch
    {
        "detractor" => 0,
        "passive" => 1,
        _ => 2
    };

    private void ResultsKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            CloseResultExpansion();
        }
    }

    private void CloseResultExpansion()
    {
        expansionCancellation?.Cancel();
        expandedProjectId = null;
        expandedResponses = [];
        expansionError = null;
        expansionLoading = false;
    }

    private void OpenResponseDialog(NpsResponseView response)
    {
        selectedResponse = response;
        responseDialogOpen = true;
    }

    private void ResponseRowKeyDown(
        Microsoft.AspNetCore.Components.Web.KeyboardEventArgs args,
        NpsResponseView response)
    {
        if (args.Key is "Enter" or " ")
        {
            OpenResponseDialog(response);
        }
    }

    private void CloseResponseDialog() => responseDialogOpen = false;

    private static string ResponseAuthorName(NpsResponseView response)
    {
        if (!string.IsNullOrWhiteSpace(response.RespondentName))
        {
            return response.RespondentName;
        }

        if (!string.IsNullOrWhiteSpace(response.RespondentEmail))
        {
            return response.RespondentEmail;
        }

        if (!string.IsNullOrWhiteSpace(response.ContactName))
        {
            return response.ContactName;
        }

        return !string.IsNullOrWhiteSpace(response.ContactEmail)
            ? response.ContactEmail
            : "Resposta anônima";
    }

    private static string? ResponseAuthorDetail(NpsResponseView response)
    {
        if (!string.IsNullOrWhiteSpace(response.RespondentName) && !string.IsNullOrWhiteSpace(response.RespondentEmail))
        {
            return response.RespondentEmail;
        }

        if (string.IsNullOrWhiteSpace(response.RespondentName) &&
            string.IsNullOrWhiteSpace(response.RespondentEmail) &&
            !string.IsNullOrWhiteSpace(response.ContactName) &&
            !string.IsNullOrWhiteSpace(response.ContactEmail))
        {
            return response.ContactEmail;
        }

        return null;
    }

    private static string ResponseAspectAverage(NpsResponseView response)
    {
        var values = new[] { response.Quality, response.Schedule, response.Communication, response.BusinessValue }
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();
        return values.Length == 0
            ? "—"
            : values.Average().ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
    }

    private async Task ChangeTabAsync(NpsTab tab)
    {
        if (tab == ActiveTab)
        {
            return;
        }

        CloseResultExpansion();
        ActiveTab = tab;
        NavigationManager.NavigateTo(TabHref(TabCode(tab)));
        ReadQuery();
        await ReloadAsync();
    }

    private string TabHref(string tab) => $"/nps/{tab}{CurrentRawQuery()}";
    private string PublicUrl(Guid token) => NavigationManager.ToAbsoluteUri($"/nps/{token}").ToString();
    private ValueTask CopyAsync(string value) => JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", value);
    private static string DisplayMetric(double? value) => value?.ToString("0.0") ?? "—";
    private static string DisplayAspectAverage(double? value)
        => value?.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) ?? "—";
    private static string AspectMeterValue(double value)
        => value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    private static string AspectMeterLabel(NpsAspectAverageView aspect, int maximum)
        => $"{aspect.Label}: média {DisplayAspectAverage(aspect.Average)} de {maximum}, {aspect.ResponsesCount} respostas.";

    private void ReadQuery()
    {
        var query = ParseQuery(CurrentRawQuery());
        search = First(query, "search") ?? string.Empty;
        Replace(clients, query, "client");
        Replace(dcs, query, "dc");
        Replace(projectTypes, query, "projectType");
        Replace(deliveryManagers, query, "deliveryManager");
        Replace(statuses, query, "status");
        Replace(formats, query, "format");
        Replace(classifications, query, "classification");
        from = NullIfEmpty(First(query, "from"));
        to = NullIfEmpty(First(query, "to"));
        includeWaived = bool.TryParse(First(query, "includeWaived"), out var parsedWaived) && parsedWaived;
    }

    private string BuildQuery()
    {
        var values = new List<KeyValuePair<string, string>>();
        Add(values, "search", NullIfEmpty(search));
        Add(values, "client", clients);
        Add(values, "dc", dcs);
        Add(values, "projectType", projectTypes);
        Add(values, "deliveryManager", deliveryManagers);
        Add(values, "status", statuses);
        Add(values, "format", formats);
        Add(values, "classification", classifications);
        Add(values, "from", from);
        Add(values, "to", to);
        if (includeWaived)
        {
            values.Add(new("includeWaived", "true"));
        }

        return values.Count == 0
            ? string.Empty
            : $"?{string.Join('&', values.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"))}";
    }

    private string BuildResponseQuery()
    {
        var values = new List<KeyValuePair<string, string>>();
        Add(values, "search", NullIfEmpty(search));
        Add(values, "client", clients);
        Add(values, "dc", dcs);
        Add(values, "projectType", projectTypes);
        Add(values, "deliveryManager", deliveryManagers);
        Add(values, "format", formats);
        Add(values, "classification", classifications);
        Add(values, "from", from);
        Add(values, "to", to);
        return values.Count == 0
            ? string.Empty
            : $"?{string.Join('&', values.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"))}";
    }

    private string CurrentRawQuery() => NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query;

    private static Dictionary<string, List<string>> ParseQuery(string rawQuery)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in rawQuery.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            var key = Decode(separator < 0 ? part : part[..separator]);
            var value = Decode(separator < 0 ? string.Empty : part[(separator + 1)..]);
            if (!result.TryGetValue(key, out var values))
            {
                values = [];
                result[key] = values;
            }

            values.Add(value);
        }

        return result;
    }

    private static string? First(IReadOnlyDictionary<string, List<string>> query, string key)
        => query.TryGetValue(key, out var values) ? values.FirstOrDefault() : null;

    private static string Decode(string value) => Uri.UnescapeDataString(value.Replace('+', ' '));

    private static void Replace(HashSet<string> target, IReadOnlyDictionary<string, List<string>> query, string key)
    {
        target.Clear();
        if (query.TryGetValue(key, out var values))
        {
            foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                target.Add(value!);
            }
        }
    }

    private static void Add(List<KeyValuePair<string, string>> values, string key, IEnumerable<string> facet)
    {
        foreach (var value in facet.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            values.Add(new(key, value));
        }
    }

    private static void Add(List<KeyValuePair<string, string>> values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values.Add(new(key, value));
        }
    }

    private static void AddChip(List<FilterChip> chips, string key, string label, IReadOnlyCollection<string> values)
    {
        if (values.Count != 0)
        {
            chips.Add(new FilterChip(key, label, string.Join(", ", values.OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase))));
        }
    }

    private static string TabCode(NpsTab tab) => tab switch
    {
        NpsTab.Results => "resultados",
        NpsTab.Responses => "respostas",
        _ => "coleta"
    };

    private static BrqStatusTone Tone(string tone) => tone switch
    {
        "positive" => BrqStatusTone.Positive,
        "warning" => BrqStatusTone.Warning,
        "critical" => BrqStatusTone.Danger,
        "info" => BrqStatusTone.Info,
        _ => BrqStatusTone.Neutral
    };

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void Dispose()
    {
        loadCancellation?.Cancel();
        loadCancellation?.Dispose();
        searchCancellation?.Cancel();
        searchCancellation?.Dispose();
        expansionCancellation?.Cancel();
        expansionCancellation?.Dispose();
    }

    private sealed record BoardColumn(string Code, string Label, string ColorClass);
    private sealed record FilterChip(string Key, string Label, string Values);
}

public enum NpsTab
{
    Collection,
    Results,
    Responses
}
