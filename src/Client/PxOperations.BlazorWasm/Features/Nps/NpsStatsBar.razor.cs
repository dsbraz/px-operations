using Microsoft.AspNetCore.Components;

namespace PxOperations.BlazorWasm.Features.Nps;

public partial class NpsStatsBar : ComponentBase
{
    /// <summary>Nulo enquanto o painel não carregou: a barra mostra travessões.</summary>
    [Parameter] public double? OfficialNps { get; set; }

    [Parameter] public int? TotalResponses { get; set; }

    [Parameter] public double? AverageScore { get; set; }

    [Parameter] public int? OverdueProjects { get; set; }
}
