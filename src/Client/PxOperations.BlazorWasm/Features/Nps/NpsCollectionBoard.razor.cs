using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Nps;

public partial class NpsCollectionBoard : ComponentBase
{
    private static readonly IReadOnlyList<BoardColumn> BoardColumns =
    [
        new("no_link", "Sem link", "kb-gray"),
        new("awaiting_response", "Aguardando resposta", "kb-orange"),
        new("recollection", "Recoleta", "kb-purple"),
        new("current", "Em dia", "kb-green")
    ];

    [Parameter] public IReadOnlyList<NpsProjectView> Projects { get; set; } = [];
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public string? LoadError { get; set; }
    [Parameter] public EventCallback OnReload { get; set; }
    [Parameter] public bool IncludeWaived { get; set; }
    [Parameter] public EventCallback<NpsProjectView> OnPrimaryAction { get; set; }
    [Parameter] public EventCallback<int> OnOpenDetail { get; set; }
    [Parameter] public EventCallback<int> OnOpenWaiver { get; set; }
    [Parameter] public EventCallback<int> OnReactivate { get; set; }

    private IReadOnlyList<NpsProjectView> WaivedProjects
        => Projects.Where(project => project.Stage.Code == "waived").ToArray();

    private IReadOnlyList<NpsProjectView> ProjectsForStage(string stage)
        => Projects.Where(project => project.Stage.Code == stage).ToArray();

    private sealed record BoardColumn(string Code, string Label, string ColorClass);
}
