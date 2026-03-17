using System.Globalization;
using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Projects;

public partial class ProjectRenewalsView : ComponentBase
{
    [Parameter, EditorRequired] public List<ProjectResponse> Projects { get; set; } = [];
    [Parameter] public EventCallback<int> OnEdit { get; set; }

    private string renovYear   = "2026";
    private string renovPeriod = "ano";

    private bool InRenovPeriod(string? endDate)
    {
        if (endDate is null) return false;
        if (!DateTime.TryParse(endDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return false;
        if (!int.TryParse(renovYear, out var year) || d.Year != year) return false;
        return renovPeriod switch
        {
            "q1" => d.Month <= 3,
            "q2" => d.Month >= 4 && d.Month <= 6,
            "q3" => d.Month >= 7 && d.Month <= 9,
            "q4" => d.Month >= 10,
            _    => true
        };
    }

    private List<ProjectResponse> Scope => Projects
        .Where(p => InRenovPeriod(p.EndDate))
        .ToList();

    private List<string> ScopedDcs => Scope
        .Select(p => p.Dc)
        .Distinct()
        .OrderBy(d => d)
        .ToList();

    private bool ShowDcBars => ScopedDcs.Count > 1;

    private int Total       => Scope.Count;
    private int Approved    => Scope.Count(p => p.Renewal == "Aprovada");
    private int InProgress  => Scope.Count(p => p.Renewal == "Em andamento");
    private int Pending     => Scope.Count(p => p.Renewal == "Pendente");
    private int NoStatus    => Total - Approved - InProgress - Pending;
    private int CoveragePct => Total == 0 ? 0 : (Approved + InProgress + Pending) * 100 / Total;

    private string PeriodLabel => renovPeriod switch
    {
        "q1" => "Q1", "q2" => "Q2", "q3" => "Q3", "q4" => "Q4",
        _    => "Ano completo"
    };

    private List<(string Dc, int Total, int WithStatus, int Approved, int Pct)> DcBars => ScopedDcs
        .Select(dc =>
        {
            var scoped     = Scope.Where(p => p.Dc == dc).ToList();
            var total      = scoped.Count;
            var withStatus = scoped.Count(p => p.Renewal is not ("None" or null or ""));
            var approved   = scoped.Count(p => p.Renewal == "Aprovada");
            var pct        = total == 0 ? 0 : withStatus * 100 / total;
            return (dc, total, withStatus, approved, pct);
        })
        .ToList();

    private List<ProjectResponse> Cards => Scope
        .Where(p => p.Renewal is not ("None" or null or ""))
        .ToList();
}
