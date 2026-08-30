using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Nps;

public partial class NpsToolbar : ComponentBase
{
    [Parameter] public bool ShowsFilters { get; set; }
    [Parameter] public NpsTab ActiveTab { get; set; }
    [Parameter] public NpsFilterOptionsView FilterOptions { get; set; } = new();

    [Parameter] public string Search { get; set; } = string.Empty;
    [Parameter] public string SearchPlaceholder { get; set; } = string.Empty;
    [Parameter] public EventCallback<ChangeEventArgs> OnSearchInput { get; set; }

    [Parameter] public int ActiveFacetCount { get; set; }
    [Parameter] public EventCallback OnClear { get; set; }

    // Os conjuntos chegam concretos de propósito: eles comparam sem diferenciar
    // maiúsculas, e recebê-los por uma interface faria o Contains cair na
    // sobrecarga do LINQ, que compara ordinal — a marcação da faceta passaria a
    // depender da caixa do código vindo do servidor.
    [Parameter] public HashSet<string> Clients { get; set; } = [];
    [Parameter] public HashSet<string> Dcs { get; set; } = [];
    [Parameter] public HashSet<string> ProjectTypes { get; set; } = [];
    [Parameter] public HashSet<string> DeliveryManagers { get; set; } = [];
    [Parameter] public HashSet<string> Statuses { get; set; } = [];
    [Parameter] public HashSet<string> Formats { get; set; } = [];
    [Parameter] public HashSet<string> Classifications { get; set; } = [];
    [Parameter] public EventCallback<NpsFacetToggle> OnToggleFacet { get; set; }

    [Parameter] public bool IncludeWaived { get; set; }
    [Parameter] public EventCallback OnToggleWaived { get; set; }

    [Parameter] public string? From { get; set; }
    [Parameter] public string? To { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnFromChanged { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnToChanged { get; set; }

    [Parameter] public string CollectionHref { get; set; } = string.Empty;
    [Parameter] public string ResultsHref { get; set; } = string.Empty;
    [Parameter] public string ResponsesHref { get; set; } = string.Empty;
    [Parameter] public EventCallback<NpsTab> OnTabChange { get; set; }

    private Task ToggleAsync(string key, string value)
        => OnToggleFacet.InvokeAsync(new NpsFacetToggle(key, value));
}
