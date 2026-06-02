using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.ProjectHealth;

public partial class ProjectHealthDetailModal : ComponentBase
{
    [Parameter, EditorRequired] public ProjectHealthResponse Entry { get; set; } = default!;
    [Parameter, EditorRequired] public EventCallback OnClose { get; set; }

    private string ScoreClass => ProjectHealthFormat.ScoreClass(Entry.Score);

    private IEnumerable<(string Label, string Value)> RagFields =>
    [
        ("Escopo", Entry.Scope),
        ("Cronograma", Entry.Schedule),
        ("Qualidade", Entry.Quality),
        ("Satisfação", Entry.Satisfaction),
    ];

    private static string RagDotClass(string rag) => rag.ToLowerInvariant() switch
    {
        "verde" or "green" => "rag-dot-green",
        "amarelo" or "yellow" => "rag-dot-yellow",
        "vermelho" or "red" => "rag-dot-red",
        _ => "rag-dot-green"
    };

    private static string Capitalize(string value) => value.Length == 0 ? value
        : char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
}
