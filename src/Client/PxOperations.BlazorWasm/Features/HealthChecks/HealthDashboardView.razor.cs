using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.HealthChecks;

public partial class HealthDashboardView : ComponentBase
{
    [Parameter, EditorRequired] public HealthCheckSummaryResponse? Summary { get; set; }
    [Parameter, EditorRequired] public List<HealthCheckResponse> Entries { get; set; } = [];

    private static string ScoreClass(int score) => score >= 7 ? "score-hi" : score >= 4 ? "score-md" : "score-lo";

    private static string FormatWeek(string week)
    {
        if (DateOnly.TryParse(week, out var d))
            return d.ToString("dd MMM", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        return week;
    }
}
