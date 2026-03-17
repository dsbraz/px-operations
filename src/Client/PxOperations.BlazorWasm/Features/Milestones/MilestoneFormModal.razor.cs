using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Milestones;

public partial class MilestoneFormModal : ComponentBase
{
    private static readonly string[] milestoneTypes =
    [
        "Apresentação Sponsor",
        "Entrega Final",
        "Presencial com Cliente",
        "Kickoff",
        "Outros"
    ];

    [Inject] private MilestonesClient MilestonesClient { get; set; } = default!;

    [Parameter, EditorRequired] public IReadOnlyList<ProjectResponse> Projects { get; set; } = [];
    [Parameter] public MilestoneResponse? EditingMilestone { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<MilestoneResponse> OnSaved { get; set; }

    private bool isSaving;
    private string? errorMessage;
    private int formProjectId;
    private string formType = milestoneTypes[0];
    private string formTitle = string.Empty;
    private DateTime formDate = DateTime.Today;
    private TimeOnly? formTime;
    private string? formNotes;

    protected override void OnParametersSet()
    {
        if (Projects.Count == 0)
            return;

        if (EditingMilestone is null)
        {
            formProjectId = formProjectId == 0 ? Projects[0].Id : formProjectId;
            formType = milestoneTypes[0];
            formTitle = string.Empty;
            formDate = DateTime.Today;
            formTime = null;
            formNotes = null;
            errorMessage = null;
            return;
        }

        formProjectId = EditingMilestone.ProjectId;
        formType = EditingMilestone.Type;
        formTitle = EditingMilestone.Title;
        formDate = DateOnly.Parse(EditingMilestone.Date).ToDateTime(TimeOnly.MinValue);
        formTime = string.IsNullOrWhiteSpace(EditingMilestone.Time) ? null : TimeOnly.Parse(EditingMilestone.Time);
        formNotes = EditingMilestone.Notes;
        errorMessage = null;
    }

    private async Task SaveAsync()
    {
        if (Projects.Count == 0)
        {
            errorMessage = "Nenhum projeto disponível para vincular o marco.";
            return;
        }

        if (string.IsNullOrWhiteSpace(formTitle))
        {
            errorMessage = "Informe o título do marco.";
            return;
        }

        isSaving = true;
        errorMessage = null;

        try
        {
            MilestoneResponse saved;

            if (EditingMilestone is null)
            {
                saved = await MilestonesClient.CreateAsync(new CreateMilestoneRequest
                {
                    ProjectId = formProjectId,
                    Type = formType,
                    Title = formTitle,
                    Date = DateOnly.FromDateTime(formDate).ToString("yyyy-MM-dd"),
                    Time = formTime?.ToString("HH\\:mm"),
                    Notes = string.IsNullOrWhiteSpace(formNotes) ? null : formNotes
                });
            }
            else
            {
                saved = await MilestonesClient.UpdateAsync(EditingMilestone.Id, new UpdateMilestoneRequest
                {
                    ProjectId = formProjectId,
                    Type = formType,
                    Title = formTitle,
                    Date = DateOnly.FromDateTime(formDate).ToString("yyyy-MM-dd"),
                    Time = formTime?.ToString("HH\\:mm"),
                    Notes = string.IsNullOrWhiteSpace(formNotes) ? null : formNotes
                });
            }

            await OnSaved.InvokeAsync(saved);
        }
        catch (Exception ex)
        {
            errorMessage = ApiErrorFormatter.Format(ex, "Não foi possível salvar o marco.");
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task CloseOnOverlay()
    {
        await OnClose.InvokeAsync();
    }
}
