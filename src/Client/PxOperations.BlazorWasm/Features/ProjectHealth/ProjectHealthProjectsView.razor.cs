using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.ProjectHealth;

public partial class ProjectHealthProjectsView : ComponentBase
{
    [Parameter, EditorRequired] public List<ProjectHealthResponse> Entries { get; set; } = [];

    private HashSet<int> expandedProjects = [];

    private record ProjectGroup(int ProjectId, List<ProjectHealthResponse> Entries);

    private List<ProjectGroup> ProjectGroups =>
        Entries.GroupBy(e => e.ProjectId)
            .Select(g => new ProjectGroup(g.Key, g.ToList()))
            .OrderBy(g => g.Entries.First().ProjectName)
            .ToList();

    private void ToggleProject(int projectId)
    {
        if (!expandedProjects.Remove(projectId))
            expandedProjects.Add(projectId);
    }

    private static string ScoreClass(int score) => score >= 7 ? "score-hi" : score >= 4 ? "score-md" : "score-lo";

    private static string RagDotClass(string rag) => rag.ToLowerInvariant() switch
    {
        "verde" or "green" => "rag-dot-green",
        "amarelo" or "yellow" => "rag-dot-yellow",
        "vermelho" or "red" => "rag-dot-red",
        _ => "rag-dot-green"
    };

    private static string FormatWeek(string week)
    {
        if (DateOnly.TryParse(week, out var d))
            return d.ToString("dd MMM", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        return week;
    }
}
