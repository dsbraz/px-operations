using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Nps;

public partial class NpsResultsView : ComponentBase, IDisposable
{
    private CancellationTokenSource? expansionCancellation;
    private string resultSort = "project";
    private bool resultSortDescending;
    private int? expandedProjectId;
    private List<NpsResponseView> expandedResponses = [];
    private bool expansionLoading;
    private string? expansionError;

    [Inject] private NpsClient NpsClient { get; set; } = default!;

    [Parameter] public NpsDashboardView? Dashboard { get; set; }
    [Parameter] public IReadOnlyList<NpsProjectResultView> Results { get; set; } = [];
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public string? LoadError { get; set; }
    [Parameter] public EventCallback OnReload { get; set; }

    /// <summary>Recorte de período em vigor, repassado à expansão de uma linha.</summary>
    [Parameter] public string? From { get; set; }

    [Parameter] public string? To { get; set; }

    /// <summary>
    /// A página fecha a expansão quando o filtro muda ou a aba troca: a linha
    /// aberta deixa de fazer sentido quando o recorte por baixo dela mudou.
    /// </summary>
    public void CloseExpansion()
    {
        expansionCancellation?.Cancel();
        expandedProjectId = null;
        expandedResponses = [];
        expansionError = null;
        expansionLoading = false;
        StateHasChanged();
    }

    public void Dispose()
    {
        expansionCancellation?.Cancel();
        expansionCancellation?.Dispose();
    }

    private IEnumerable<NpsProjectResultView> SortedResults => resultSort switch
    {
        "client" => Order(result => result.Client ?? string.Empty),
        "dc" => Order(result => result.Dc),
        "responses" => Order(result => result.ResponsesCount),
        "nps" => Order(result => result.OfficialNps ?? double.MinValue),
        _ => Order(result => result.Name)
    };

    private IEnumerable<NpsProjectResultView> Order<TKey>(Func<NpsProjectResultView, TKey> key)
        => resultSortDescending
            ? Results.OrderByDescending(key).ThenBy(result => result.Name, StringComparer.CurrentCultureIgnoreCase)
            : Results.OrderBy(key).ThenBy(result => result.Name, StringComparer.CurrentCultureIgnoreCase);

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

        CloseExpansion();
    }

    private string AriaSort(string column)
        => resultSort != column ? "none" : resultSortDescending ? "descending" : "ascending";

    private async Task ToggleExpansionAsync(NpsProjectResultView result)
    {
        if (expandedProjectId == result.Id)
        {
            CloseExpansion();
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
                null, [], [], [], [], [], [], [], From, To, null, null,
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

    private Task RetryExpansionAsync(NpsProjectResultView result)
    {
        expandedProjectId = null;
        return ToggleExpansionAsync(result);
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

    private void ResultsKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            CloseExpansion();
        }
    }

    private static string DisplayAspectAverage(double? value)
        => value?.ToString("0.0", CultureInfo.GetCultureInfo("pt-BR")) ?? "—";

    // Interpolar double em cultura corrente gerava "width:33,333%" em pt-BR:
    // declaração inválida, descartada pelo navegador, barra com largura zero.
    private static string BarWidth(double percentage)
        => string.Create(CultureInfo.InvariantCulture, $"width:{percentage}%");

    private static string AspectMeterValue(double value)
        => value.ToString("0.0", CultureInfo.InvariantCulture);

    private static string AspectMeterLabel(NpsAspectAverageView aspect, int maximum)
        => $"{aspect.Label}: média {DisplayAspectAverage(aspect.Average)} de {maximum}, {aspect.ResponsesCount} respostas.";
}
