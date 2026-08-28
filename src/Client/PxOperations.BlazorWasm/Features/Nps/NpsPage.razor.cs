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
    private bool redirecting;
    private bool isLoading;
    private string? loadError;
    private string search = string.Empty;
    private string? from;
    private string? to;
    private bool includeWaived;
    private NpsDashboardView? dashboard;
    private List<NpsProjectView> projects = [];

    private bool createDialogOpen;
    private int createProjectId;
    private string createFormat = "complete";
    private string createLanguage = "pt";
    private NpsDispatchDetailView? createdDispatch;
    private bool detailDialogOpen;
    private NpsProjectDetailView? selectedDetail;
    private List<NpsResponseView> detailResponses = [];
    private bool waiverDialogOpen;
    private int waiverProjectId;
    private string waiverReason = string.Empty;

    private static readonly IReadOnlyList<BoardColumn> BoardColumns =
    [
        new("no_link", "Sem link"),
        new("awaiting_response", "Aguardando resposta"),
        new("recollection", "Recoleta"),
        new("current", "Em dia")
    ];

    private NpsTab ActiveTab { get; set; }
    private bool ShowsFilters => ActiveTab is NpsTab.Collection or NpsTab.Results;
    private NpsFilterOptionsView FilterOptions => dashboard?.FilterOptions ?? new NpsFilterOptionsView();
    private IReadOnlyList<NpsProjectView> WaivedProjects => projects.Where(project => project.Stage.Code == "waived").ToArray();
    private string CreateDialogDescription => createdDispatch is null
        ? "Escolha a rodada que será compartilhada."
        : "O link usa a validade devolvida pelo servidor.";
    private string ExportHref => new Uri(HttpClient.BaseAddress!, $"api/nps/responses/export{BuildQuery()}").ToString();
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
        ActiveTab = path.EndsWith("/resultados", StringComparison.Ordinal)
            ? NpsTab.Results
            : path.EndsWith("/respostas", StringComparison.Ordinal)
                ? NpsTab.Responses
                : NpsTab.Collection;
        ReadQuery();
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (redirecting || ActiveTab == NpsTab.Responses)
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
                var loadedDashboard = await LoadDashboardAsync(ct);
                if (version == loadVersion)
                {
                    dashboard = loadedDashboard;
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
            ActiveTab == NpsTab.Results ? formats : [],
            ActiveTab == NpsTab.Results ? classifications : [],
            ActiveTab == NpsTab.Results ? from : null,
            ActiveTab == NpsTab.Results ? to : null,
            includeWaived,
            null,
            ct);

    private Task<ICollection<NpsProjectView>> LoadProjectsAsync(CancellationToken ct)
        => NpsClient.ListProjectsAsync(
            NullIfEmpty(search), clients, dcs, projectTypes, deliveryManagers,
            [], [], [], null, null, includeWaived, null, ct);

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
            case "period": from = null; to = null; break;
            case "includeWaived": includeWaived = false; break;
        }

        await UpdateUrlAndReloadAsync();
    }

    private async Task UpdateUrlAndReloadAsync()
    {
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
        detailDialogOpen = true;
    }

    private async Task FilterDetailResponsesAsync(IReadOnlyList<string> selectedFormats)
    {
        if (selectedDetail is null)
        {
            return;
        }

        detailResponses = (await NpsClient.ListProjectResponsesAsync(
            selectedDetail.Project.Id,
            selectedFormats)).ToList();
    }

    private Task FilterAllDetailResponsesAsync() => FilterDetailResponsesAsync([]);
    private Task FilterCompleteDetailResponsesAsync() => FilterDetailResponsesAsync(["complete"]);
    private Task FilterSimplifiedDetailResponsesAsync() => FilterDetailResponsesAsync(["simplified"]);

    private void OpenWaiverDialog(int projectId)
    {
        waiverProjectId = projectId;
        waiverReason = string.Empty;
        waiverDialogOpen = true;
    }

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

    private string TabHref(string tab) => $"/nps/{tab}{CurrentRawQuery()}";
    private string PublicUrl(Guid token) => NavigationManager.ToAbsoluteUri($"/nps/{token}").ToString();
    private ValueTask CopyAsync(string value) => JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", value);
    private static string DisplayMetric(double? value) => value?.ToString("0.0") ?? "—";

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
    }

    private sealed record BoardColumn(string Code, string Label);
    private sealed record FilterChip(string Key, string Label, string Values);
}

public enum NpsTab
{
    Collection,
    Results,
    Responses
}
