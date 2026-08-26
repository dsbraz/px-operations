using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps.Components;

namespace PxOperations.BlazorWasm.Features.Nps;

public partial class NpsPage : ComponentBase, IDisposable
{
    [Inject] private NpsClient NpsClient { get; set; } = default!;
    [Inject] private HttpClient HttpClient { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    private NpsDashboardResponse? dashboard;
    private List<NpsProjectResponse> projects = [];
    private List<NpsSurveyResponse> responses = [];
    private NpsSurveyResponse? selectedResponse;
    private NpsProjectDetailResponse? selectedDetail;
    private bool isLoading = true;
    private string? loadError;
    private string? operationError;
    private int? selectedProjectId;
    private bool showCreateLinkModal;
    private bool showDetailModal;

    // D11: faceta de lista guarda um CONJUNTO. Marcar dois valores filtra pela
    // união deles; entre facetas diferentes vale a interseção, que o servidor
    // já produz ao aplicar as cláusulas em AND.
    private readonly HashSet<string> filterCompanies = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> filterDcs = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> filterDeliveryManagers = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> filterProjectTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> filterStatuses = new(StringComparer.OrdinalIgnoreCase);
    // F10: facetas da aba Respostas. Formato e classificação são por RESPOSTA,
    // então não fazem sentido nas outras abas, que listam projetos.
    private readonly HashSet<string> filterFormats = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> filterClassifications = new(StringComparer.OrdinalIgnoreCase);
    private string filterSearch = "";
    private bool includeDismissed;

    // D11: período é INTERVALO, então é single — marcar "30 dias" e "6 meses"
    // ao mesmo tempo não quer dizer nada.
    private string filterPeriod = "";

    /// <summary>
    /// Empresa e DM são texto livre: as opções vêm do servidor, não da lista
    /// exibida, que já chega filtrada.
    /// </summary>
    private NpsFilterOptionsResponse? filterOptions;

    private CancellationTokenSource? searchDebounce;

    /// <summary>Espera entre a última tecla e a busca no servidor.</summary>
    private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(300);

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
    private static readonly string[] StatusOptions = ["Respondido", "Link gerado", "Pendente"];
    private static readonly string[] FormatOptions = ["Completo", "Simplificado"];
    private static readonly string[] ClassificationOptions = ["Promotor", "Neutro", "Detrator"];

    /// <summary>
    /// F1: períodos prontos mais intervalo livre. A régua é a data da RESPOSTA,
    /// por isso a faceta não é oferecida na Coleta: lá esvaziaria justamente as
    /// colunas de quem ainda não respondeu, que é o trabalho da tela.
    /// </summary>
    private static readonly (string Value, string Label)[] PeriodOptions =
    [
        ("30d", "Últimos 30 dias"),
        ("90d", "Últimos 90 dias"),
        ("6m", "Últimos 6 meses"),
        ("12m", "Últimos 12 meses")
    ];

    private bool OffersDateFacet => ActiveTab != TabCollection;
    private bool OffersStatusFacet => ActiveTab == TabResults;
    private bool OffersResponseFacets => ActiveTab == TabResponses;

    // O protótipo abre em Coleta: é a tela de trabalho do operador.
    private string ActiveTab => Tab switch
    {
        TabResults => TabResults,
        TabResponses => TabResponses,
        _ => TabCollection
    };

    private string PortfolioCount => ActiveTab == TabResponses
        ? $"{responses.Count} {(responses.Count == 1 ? "resposta" : "respostas")}"
        : $"{projects.Count} projetos";

    private IReadOnlyList<NpsFilterBar.NpsFilterChip> FilterChips
    {
        get
        {
            // F1: um chip por FACETA, juntando os valores — dois chips "DC"
            // lado a lado não diriam se é união ou interseção.
            var chips = new List<NpsFilterBar.NpsFilterChip>();
            AddFacetChip(chips, "Empresa", filterCompanies);
            AddFacetChip(chips, "DC", filterDcs);
            AddFacetChip(chips, "Tipo", filterProjectTypes);
            AddFacetChip(chips, "DM", filterDeliveryManagers);
            if (OffersStatusFacet) AddFacetChip(chips, "Status", filterStatuses);
            if (OffersResponseFacets)
            {
                AddFacetChip(chips, "Formato", filterFormats);
                AddFacetChip(chips, "Classificação", filterClassifications);
            }
            if (OffersDateFacet && PeriodLabel is { } period) chips.Add(new("Período", period, "period"));
            if (includeDismissed) chips.Add(new("Dispensados", "Mostrando", "dismissed"));
            return chips;
        }
    }
    private static void AddFacetChip(List<NpsFilterBar.NpsFilterChip> chips, string facet, IReadOnlyCollection<string> values)
    {
        if (values.Count > 0)
        {
            chips.Add(new(facet, string.Join(", ", values), facet));
        }
    }

    private string? PeriodLabel
        => PeriodOptions.FirstOrDefault(o => o.Value == filterPeriod).Label;

    /// <summary>
    /// Passo 2 do F3: quando existe, o modal deixa de perguntar e passa a
    /// entregar — URL, validade em destaque e mensagem pronta para colar.
    /// </summary>
    private NpsDispatchDetailResponse? createdDispatch;

    private NpsProjectResponse? dismissTarget;

    private string ExportHref => BuildExportUrl();

    protected override async Task OnInitializedAsync()
    {
        // As opções não dependem do filtro corrente, então uma carga só basta.
        // Falhar aqui não pode derrubar a tela: sem elas as facetas de texto
        // livre ficam vazias, o resto segue funcionando.
        try
        {
            filterOptions = await NpsClient.GetFilterOptionsAsync();
        }
        catch (Exception)
        {
            filterOptions = null;
        }

        await RefreshAsync();
    }

    /// <summary>
    /// A busca é por tecla. Sem espera, digitar "acme" lança quatro rodadas de
    /// 2 a 3 requisições e vale a que voltar por último — a lista podia ficar
    /// com o resultado de "acm" e a caixa mostrando "acme".
    /// </summary>
    private async Task OnSearchChanged(string value)
    {
        filterSearch = value;

        searchDebounce?.Cancel();
        searchDebounce?.Dispose();
        var pending = new CancellationTokenSource();
        searchDebounce = pending;

        try
        {
            await Task.Delay(SearchDebounce, pending.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await RefreshAsync();
    }

    private Task ToggleCompany(string value, ChangeEventArgs args) => ToggleFacet(filterCompanies, value, args);

    private Task ToggleDc(string value, ChangeEventArgs args) => ToggleFacet(filterDcs, value, args);

    private Task ToggleProjectType(string value, ChangeEventArgs args) => ToggleFacet(filterProjectTypes, value, args);

    private Task ToggleDeliveryManager(string value, ChangeEventArgs args) => ToggleFacet(filterDeliveryManagers, value, args);

    private Task ToggleStatus(string value, ChangeEventArgs args) => ToggleFacet(filterStatuses, value, args);

    private Task ToggleFormat(string value, ChangeEventArgs args) => ToggleFacet(filterFormats, value, args);

    private Task ToggleClassification(string value, ChangeEventArgs args) => ToggleFacet(filterClassifications, value, args);

    private async Task ToggleFacet(HashSet<string> facet, string value, ChangeEventArgs args)
    {
        if (IsChecked(args))
        {
            facet.Add(value);
        }
        else
        {
            facet.Remove(value);
        }

        await RefreshAsync();
    }

    /// <summary>
    /// Período é radio, não checkbox: marcar o que já está marcado desliga, que
    /// é como se limpa um intervalo sem um botão só para isso.
    /// </summary>
    private async Task SelectPeriod(string value)
    {
        filterPeriod = filterPeriod == value ? string.Empty : value;
        await RefreshAsync();
    }

    private async Task ToggleDismissed(ChangeEventArgs args)
    {
        includeDismissed = IsChecked(args);
        await RefreshAsync();
    }

    private async Task RemoveChip(NpsFilterBar.NpsFilterChip chip)
    {
        switch (chip.Value)
        {
            case "dismissed": includeDismissed = false; break;
            case "period": filterPeriod = string.Empty; break;
            case "Empresa": filterCompanies.Clear(); break;
            case "DC": filterDcs.Clear(); break;
            case "Tipo": filterProjectTypes.Clear(); break;
            case "DM": filterDeliveryManagers.Clear(); break;
            case "Status": filterStatuses.Clear(); break;
            case "Formato": filterFormats.Clear(); break;
            case "Classificação": filterClassifications.Clear(); break;
        }

        await RefreshAsync();
    }

    /// <summary>
    /// "Limpar tudo" limpa a BUSCA também. Deixá-la de fora escondia a linha de
    /// chips e mantinha a lista filtrada, sem nada na tela indicando por quê.
    /// </summary>
    private async Task ClearFilters()
    {
        filterSearch = string.Empty;
        filterPeriod = string.Empty;
        filterCompanies.Clear();
        filterDcs.Clear();
        filterProjectTypes.Clear();
        filterDeliveryManagers.Clear();
        filterStatuses.Clear();
        filterFormats.Clear();
        filterClassifications.Clear();
        includeDismissed = false;
        await RefreshAsync();
    }

    private static bool IsChecked(ChangeEventArgs args)
        => args.Value is true || (bool.TryParse(args.Value?.ToString(), out var parsed) && parsed);

    private async Task OnCreateLinkProjectChanged(int? projectId)
    {
        if (projectId is null)
        {
            selectedProjectId = null;
            selectedDetail = null;
            return;
        }

        await SelectProjectAsync(projectId.Value);
    }

    private Task StartDismissAsync(NpsProjectResponse project)
    {
        dismissTarget = project;
        return Task.CompletedTask;
    }

    private void CancelDismiss() => dismissTarget = null;

    /// <summary>
    /// O motivo vem do diálogo, que também é quem valida e mostra o erro. A
    /// página não guarda cópia do texto: era assim que o valor se perdia.
    /// </summary>
    private async Task ConfirmDismissAsync(string reason)
    {
        if (dismissTarget is null)
        {
            return;
        }

        try
        {
            await NpsClient.DismissCollectionAsync(dismissTarget.Id, new DismissNpsCollectionRequest
            {
                Reason = reason
            });
            dismissTarget = null;
            await RefreshAsync();
        }
        catch (Exception)
        {
            operationError = "Não foi possível dispensar a coleta.";
        }
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

    /// <summary>F5: copiar o link de um disparo listado no detalhe.</summary>
    private async Task CopyDispatchLinkAsync(int dispatchId)
    {
        try
        {
            var detail = await NpsClient.GetDispatchAsync(dispatchId);
            // Prefere o alvo genérico: token nominal é de uso único e atribui
            // a resposta àquele contato. Colado numa mensagem de grupo, só a
            // primeira pessoa conseguiria responder.
            var target = detail.Targets.FirstOrDefault(t => t.IsGeneric) ?? detail.Targets.FirstOrDefault();
            if (target is not null)
            {
                await CopyAsync(BuildPublicFormUrl(target.Token), "Não foi possível copiar o link.");
            }
        }
        catch (Exception)
        {
            operationError = "Não foi possível copiar o link.";
        }
    }

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
    }

    private async Task OpenProjectDetailAsync(int projectId)
    {
        await SelectProjectAsync(projectId);
        showDetailModal = true;
    }

    private async Task CreateDispatchAsync(NpsCreateLinkModal.NpsLinkRequest request)
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
                Format = request.Format,
                Language = request.Language,
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

    private async Task LoadDashboardAndProjectsAsync()
    {
        var search = string.IsNullOrWhiteSpace(filterSearch) ? null : filterSearch.Trim();
        var (from, to) = PeriodRange();
        // Status e período só valem onde a faceta é oferecida; carregados de
        // outra aba, filtrariam por algo que não está na tela.
        var statuses = OffersStatusFacet ? Facet(filterStatuses) : null;
        var formats = OffersResponseFacets ? Facet(filterFormats) : null;
        var classifications = OffersResponseFacets ? Facet(filterClassifications) : null;

        dashboard = await NpsClient.GetDashboardAsync(
            search, Facet(filterCompanies), Facet(filterDcs), Facet(filterDeliveryManagers),
            Facet(filterProjectTypes), statuses, null, from, to, classifications, formats);

        projects = (await NpsClient.ListProjectsAsync(
            search, Facet(filterCompanies), Facet(filterDcs), Facet(filterDeliveryManagers),
            Facet(filterProjectTypes), statuses, from, to, includeDismissed)).ToList();

        // F10: só a aba Respostas consome a listagem. Carregá-la nas outras
        // seria uma requisição por troca de aba sem ninguém para ler o
        // resultado.
        if (ActiveTab == TabResponses)
        {
            responses = (await NpsClient.ListResponsesAsync(
                search, Facet(filterCompanies), Facet(filterDcs), Facet(filterDeliveryManagers),
                Facet(filterProjectTypes), null, null, from, to, classifications, formats)).ToList();
        }
    }

    /// <summary>
    /// F8: o drill-down carrega as respostas do projeto com o MESMO recorte da
    /// linha. É o critério de aceite — as notas expandidas fecham com o NPS
    /// exibido. O status fica de fora porque é faceta de projeto, e o projeto
    /// já foi escolhido.
    /// </summary>
    private async Task<IReadOnlyList<NpsSurveyResponse>> LoadProjectResponsesAsync(int projectId)
    {
        var search = string.IsNullOrWhiteSpace(filterSearch) ? null : filterSearch.Trim();
        var (from, to) = PeriodRange();

        return (await NpsClient.ListResponsesAsync(
            search, Facet(filterCompanies), Facet(filterDcs), Facet(filterDeliveryManagers),
            Facet(filterProjectTypes), null, projectId, from, to, null, null)).ToList();
    }

    private void OpenResponseDetail(NpsSurveyResponse response) => selectedResponse = response;

    private void CloseResponseDetail() => selectedResponse = null;

    /// <summary>Conjunto vazio é ausência de filtro, não filtro que não casa.</summary>
    private static IEnumerable<string>? Facet(IReadOnlyCollection<string> values)
        => values.Count == 0 ? null : values;

    /// <summary>
    /// O período vira intervalo aqui, no cliente: o servidor recebe as datas
    /// resolvidas e não precisa saber o que "últimos 90 dias" quer dizer.
    /// </summary>
    private (string? From, string? To) PeriodRange()
    {
        if (!OffersDateFacet || string.IsNullOrEmpty(filterPeriod))
        {
            return (null, null);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = filterPeriod switch
        {
            "30d" => today.AddDays(-30),
            "90d" => today.AddDays(-90),
            "6m" => today.AddMonths(-6),
            "12m" => today.AddMonths(-12),
            _ => (DateOnly?)null
        };

        return from is null ? (null, null) : (from.Value.ToString("yyyy-MM-dd"), today.ToString("yyyy-MM-dd"));
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
        // O CSV tem de corresponder ao que está na tela: sem a busca e as
        // facetas, exportar de uma lista filtrada baixava a carteira inteira.
        // selectedProjectId fica de fora de propósito — ele sobrevive ao fechar
        // o modal e não aparece na barra de filtros, então recortaria o arquivo
        // sem nada indicando isso.
        var (from, to) = PeriodRange();
        var parts = new List<string>();

        void AddSingle(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
            }
        }

        // D11: faceta de lista vai como parâmetro REPETIDO, o mesmo formato que
        // a API espera. Juntar com vírgula chegaria como um valor só.
        void AddFacet(string key, IReadOnlyCollection<string> values)
        {
            foreach (var value in values)
            {
                parts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
            }
        }

        AddSingle("search", string.IsNullOrWhiteSpace(filterSearch) ? null : filterSearch.Trim());
        AddFacet("company", filterCompanies);
        AddFacet("dc", filterDcs);
        AddFacet("deliveryManager", filterDeliveryManagers);
        AddFacet("projectType", filterProjectTypes);
        if (OffersStatusFacet) AddFacet("status", filterStatuses);
        AddSingle("from", from);
        AddSingle("to", to);

        var queryString = string.Join('&', parts);
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

    // O rótulo vem pronto do servidor (B15): derivar aqui de novo criaria uma
    // segunda definição da mesma regra, e foi assim que "Sem link" virou um
    // estado que a tabela nunca chegava a mostrar.
    private static string ProjectStatusClass(NpsProjectResponse project)
        => project.CollectionStatus switch
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
    /// <summary>
    /// Sem isto a busca pendente dispara depois do componente morrer: quem
    /// digita e navega para outra rota dentro dos 300ms deixa requisições em
    /// voo para uma tela que não existe mais.
    /// </summary>
    public void Dispose()
    {
        searchDebounce?.Cancel();
        searchDebounce?.Dispose();
        searchDebounce = null;
    }

}
