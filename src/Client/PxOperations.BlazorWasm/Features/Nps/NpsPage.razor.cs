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
    private int loadVersion;
    private NpsResultsView? resultsView;
    private bool redirecting;
    private bool isLoading;
    private string? loadError;
    private string? actionError;
    private bool isCreatingDispatch;
    private string? selfNavigatedRoute;
    private string search = string.Empty;
    private string? from;
    private string? to;
    private bool includeWaived;
    private NpsDashboardView? dashboard;
    private NpsFilterOptionsView? filterOptions;
    private List<NpsProjectView> projects = [];
    private List<NpsProjectResultView> projectResults = [];
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

    private NpsTab ActiveTab { get; set; }
    private bool ShowsFilters => true;
    private bool ShowsIndicators => ActiveTab is NpsTab.Collection or NpsTab.Results;
    private NpsFilterOptionsView FilterOptions => filterOptions ?? dashboard?.FilterOptions ?? new NpsFilterOptionsView();
    private string CreateDialogDescription => createdDispatch is null
        ? "Escolha a rodada que será compartilhada."
        : "O link usa a validade devolvida pelo servidor.";
    private string CreatedLinkUrl => createdDispatch is null
        ? string.Empty
        : PublicUrl(createdDispatch.Targets.Single(target => target.IsGeneric).Token);

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
            var expires = NpsTimeDisplay.Date(createdDispatch.Dispatch.ExpiresAt);
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
            resultsView?.CloseExpansion();
        }

        ActiveTab = nextTab;
        ReadQuery();

        // NavigateTo dispara LocationChanged, o Router repassa os parâmetros e
        // este método roda de novo para a mesma rota: sem a guarda, cada troca
        // de filtro ou de aba disparava todas as requisições duas vezes. Quem
        // navegou daqui de dentro já recarrega por conta própria.
        var route = $"{path}{CurrentRawQuery()}";
        if (selfNavigatedRoute == route)
        {
            selfNavigatedRoute = null;
            return;
        }

        selfNavigatedRoute = null;
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
            statuses, [], [], from, to, includeWaived, null, ct);

    private Task<ICollection<NpsResponseView>> LoadResponsesAsync(CancellationToken ct)
        => NpsClient.ListResponsesAsync(
            NullIfEmpty(search), clients, dcs, projectTypes, deliveryManagers,
            [], formats, classifications, from, to, includeWaived, null, ct);

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

    private string SearchPlaceholder => ActiveTab == NpsTab.Responses
        ? "Buscar projeto, pessoa ou comentário"
        : "Buscar projeto ou cliente";

    private HashSet<string> FacetFor(string key) => key switch
    {
        "client" => clients,
        "dc" => dcs,
        "projectType" => projectTypes,
        "deliveryManager" => deliveryManagers,
        "status" => statuses,
        "format" => formats,
        _ => classifications
    };

    private async Task ToggleFacetAsync(NpsFacetToggle toggle)
    {
        var facet = FacetFor(toggle.Key);
        var value = toggle.Value;

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
        resultsView?.CloseExpansion();
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
        var target = $"/nps/{TabCode(ActiveTab)}";
        selfNavigatedRoute = target;
        NavigationManager.NavigateTo(target, replace: true);
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
        resultsView?.CloseExpansion();
        var target = $"/nps/{TabCode(ActiveTab)}{BuildQuery()}";
        selfNavigatedRoute = target;
        NavigationManager.NavigateTo(target, replace: true);
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

        actionError = null;
        isCreatingDispatch = true;
        try
        {
            createdDispatch = await NpsClient.CreateDispatchAsync(new CreateNpsDispatchRequest
            {
                ProjectId = createProjectId,
                Format = createFormat,
                Language = createLanguage,
                ContactIds = []
            });
        }
        catch (Exception exception)
        {
            // O domínio recusa a criação com 409 (coleta dispensada, por
            // exemplo). Sem este catch a exceção escapava do @onclick e caía na
            // tela de erro global, com o diálogo aberto e nenhuma explicação.
            actionError = ApiErrorFormatter.Format(exception, "Não foi possível gerar o link.");
            return;
        }
        finally
        {
            isCreatingDispatch = false;
        }

        // O quadro atrás do diálogo ainda mostra o projeto em "Sem link" com a
        // ação "Gerar link": clicar de novo criaria um segundo disparo e
        // fecharia justamente o link recém-compartilhado.
        await ReloadAsync();
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

        actionError = null;
        createdDispatch = null;
        createProjectId = project.Id;
        createFormat = action.Format ?? "complete";
        createLanguage = "pt";
        createDialogOpen = true;
    }

    private async Task OpenDetailAsync(int projectId)
    {
        actionError = null;
        try
        {
            selectedDetail = await NpsClient.GetProjectAsync(projectId);
        }
        catch (Exception exception)
        {
            actionError = ApiErrorFormatter.Format(exception, "Não foi possível abrir o detalhe da coleta.");
            return;
        }

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

        actionError = null;
        detailFormat = format;
        try
        {
            detailResponses = (await NpsClient.ListProjectResponsesAsync(
                selectedDetail.Project.Id,
                null, [], [], [], [], [], selectedFormats, [], null, null, true, null)).ToList();
        }
        catch (Exception exception)
        {
            actionError = ApiErrorFormatter.Format(exception, "Não foi possível filtrar as respostas.");
        }
    }

    private Task FilterDetailByFormatAsync(string format)
        => FilterDetailResponsesAsync(format, format == "all" ? [] : [format]);

    private void OpenWaiverDialog(int projectId)
    {
        actionError = null;
        waiverProjectId = projectId;
        waiverReason = string.Empty;
        waiverDialogOpen = true;
    }

    private void CloseCreateDialog() => createDialogOpen = false;
    private void CloseDetailDialog() => detailDialogOpen = false;
    private void CloseWaiverDialog() => waiverDialogOpen = false;

    private async Task WaiveAsync()
    {
        actionError = null;
        try
        {
            await NpsClient.WaiveCollectionAsync(waiverProjectId, new WaiveNpsCollectionRequest { Reason = waiverReason });
        }
        catch (Exception exception)
        {
            // O diálogo continua aberto: o motivo digitado não pode se perder
            // porque outro operador mexeu no projeto antes.
            actionError = ApiErrorFormatter.Format(exception, "Não foi possível dispensar a coleta.");
            return;
        }

        waiverDialogOpen = false;
        await ReloadAsync();
    }

    private async Task ReactivateAsync(int projectId)
    {
        actionError = null;
        try
        {
            await NpsClient.ReactivateCollectionAsync(projectId);
        }
        catch (Exception exception)
        {
            actionError = ApiErrorFormatter.Format(exception, "Não foi possível reativar a coleta.");
            return;
        }

        await ReloadAsync();
    }

    private void OpenResponseDialog(NpsResponseView response)
    {
        selectedResponse = response;
        responseDialogOpen = true;
    }

    private void CloseResponseDialog() => responseDialogOpen = false;

    private async Task ChangeTabAsync(NpsTab tab)
    {
        if (tab == ActiveTab)
        {
            return;
        }

        resultsView?.CloseExpansion();
        ActiveTab = tab;
        var target = TabHref(TabCode(tab));
        selfNavigatedRoute = target;
        NavigationManager.NavigateTo(target);
        ReadQuery();
        await ReloadAsync();
    }

    private string TabHref(string tab) => $"/nps/{tab}{CurrentRawQuery()}";
    private string PublicUrl(Guid token) => NavigationManager.ToAbsoluteUri($"/nps/{token}").ToString();
    // Task, não ValueTask: EventCallbackFactory não tem sobrecarga para
    // Func<ValueTask>, então um @onclick que devolvia ValueTask caía na
    // sobrecarga Action e a task era descartada sem ninguém esperar por ela.
    private async Task CopyAsync(string value)
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", value);
            actionError = null;
        }
        catch (JSException)
        {
            actionError = "Não foi possível copiar. Copie manualmente do campo acima.";
        }
    }
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
        if (includeWaived)
        {
            values.Add(new("includeWaived", "true"));
        }

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

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public void Dispose()
    {
        loadCancellation?.Cancel();
        loadCancellation?.Dispose();
        searchCancellation?.Cancel();
        searchCancellation?.Dispose();
    }

    private sealed record FilterChip(string Key, string Label, string Values);
}

public enum NpsTab
{
    Collection,
    Results,
    Responses
}
