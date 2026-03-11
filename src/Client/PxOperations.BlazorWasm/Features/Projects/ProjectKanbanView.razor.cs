using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Projects;

public partial class ProjectKanbanView : ComponentBase
{
    [Inject] private ProjectsClient ProjectsClient { get; set; } = default!;

    [Parameter, EditorRequired] public List<ProjectResponse> Projects { get; set; } = [];
    [Parameter] public EventCallback<int> OnEdit { get; set; }
    [Parameter] public EventCallback<ProjectResponse> OnProjectUpdated { get; set; }
    [Parameter] public EventCallback<string> OnToast { get; set; }
    [Parameter] public EventCallback<string> OnError { get; set; }

    private string groupBy = "status";
    private ProjectResponse? draggedProject;
    private string? dragOverColumnKey;

    private List<(string Key, string Label, string ColorClass, List<ProjectResponse> Items)> Columns
    {
        get
        {
            var all = Projects;

            if (groupBy == "renewal")
                return
                [
                    ("None",         "Sem status",   "kb-gray",   all.Where(p => p.Renewal is "None" or null or "").ToList()),
                    ("Pendente",     "Pendente",     "kb-orange",  all.Where(p => p.Renewal == "Pendente").ToList()),
                    ("Em andamento", "Em andamento", "kb-blue",    all.Where(p => p.Renewal == "Em andamento").ToList()),
                    ("Aprovada",     "Aprovada",     "kb-green",   all.Where(p => p.Renewal == "Aprovada").ToList()),
                ];

            if (groupBy == "dc")
                return new[] { "DC1", "DC2", "DC3", "DC4", "DC5", "DC6" }
                    .Select(dc => (Key: dc, Label: dc, ColorClass: "kb-purple",
                                   Items: all.Where(p => p.Dc == dc).ToList()))
                    .ToList();

            return
            [
                ("Programado",   "Programado",   "kb-orange", all.Where(p => p.Status == "Programado").ToList()),
                ("Em andamento", "Em andamento", "kb-green",  all.Where(p => p.Status == "Em andamento").ToList()),
                ("Encerrado",    "Encerrado",    "kb-gray",   all.Where(p => p.Status == "Encerrado").ToList()),
            ];
        }
    }

    private void OnDragStart(ProjectResponse project)
    {
        draggedProject = project;
        dragOverColumnKey = null;
    }

    private void OnDragEnter(string columnKey) => dragOverColumnKey = columnKey;

    private void OnDragLeave(string columnKey)
    {
        if (dragOverColumnKey == columnKey)
            dragOverColumnKey = null;
    }

    private void OnDragEnd()
    {
        draggedProject = null;
        dragOverColumnKey = null;
    }

    private async Task OnDrop(string columnKey)
    {
        if (draggedProject is null) return;

        var currentKey = groupBy switch
        {
            "renewal" => draggedProject.Renewal is null or "" ? "None" : draggedProject.Renewal,
            "dc"      => draggedProject.Dc,
            _         => draggedProject.Status,
        };

        if (currentKey == columnKey) { OnDragEnd(); return; }

        var request = new UpdateProjectRequest
        {
            Dc = draggedProject.Dc, Status = draggedProject.Status,
            Name = draggedProject.Name, Client = draggedProject.Client,
            Type = draggedProject.Type, StartDate = draggedProject.StartDate,
            EndDate = draggedProject.EndDate, DeliveryManager = draggedProject.DeliveryManager,
            Renewal = draggedProject.Renewal, RenewalObservation = draggedProject.RenewalObservation,
        };

        switch (groupBy)
        {
            case "status":  request.Status  = columnKey; break;
            case "renewal": request.Renewal = columnKey; break;
            case "dc":      request.Dc      = columnKey; break;
        }

        try
        {
            var updated = await ProjectsClient.UpdateAsync(draggedProject.Id, request, default);
            await OnProjectUpdated.InvokeAsync(updated);
            await OnToast.InvokeAsync("Projeto movido com sucesso!");
        }
        catch (Exception ex)
        {
            await OnError.InvokeAsync(ex.Message);
        }
        finally
        {
            OnDragEnd();
        }
    }
}
