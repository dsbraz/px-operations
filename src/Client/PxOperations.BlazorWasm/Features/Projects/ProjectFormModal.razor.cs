using System.Globalization;
using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Projects;

public partial class ProjectFormModal : ComponentBase
{
    [Inject] private ProjectsClient ProjectsClient { get; set; } = default!;

    [Parameter] public ProjectResponse? EditingProject { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback<ProjectResponse> OnSaved { get; set; }

    private string formDc = "DC1";
    private string formStatus = "Em andamento";
    private string formName = "";
    private string? formClient;
    private string formType = "Squad";
    private DateTime? formStartDate;
    private DateTime? formEndDate;
    private string? formDeliveryManager;
    private string formRenewal = "None";
    private string? formRenewalObservation;

    private bool isSaving;
    private string? error;

    protected override void OnParametersSet()
    {
        error = null;

        if (EditingProject is not null)
        {
            formDc = EditingProject.Dc;
            formStatus = EditingProject.Status;
            formName = EditingProject.Name;
            formClient = EditingProject.Client;
            formType = EditingProject.Type;
            formStartDate = EditingProject.StartDate is not null
                ? DateTime.Parse(EditingProject.StartDate, CultureInfo.InvariantCulture)
                : null;
            formEndDate = EditingProject.EndDate is not null
                ? DateTime.Parse(EditingProject.EndDate, CultureInfo.InvariantCulture)
                : null;
            formDeliveryManager = EditingProject.DeliveryManager;
            formRenewal = EditingProject.Renewal;
            formRenewalObservation = EditingProject.RenewalObservation;
        }
        else
        {
            formDc = "DC1";
            formStatus = "Em andamento";
            formName = "";
            formClient = null;
            formType = "Squad";
            formStartDate = null;
            formEndDate = null;
            formDeliveryManager = null;
            formRenewal = "None";
            formRenewalObservation = null;
        }
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(formName))
        {
            error = "Informe o nome do projeto.";
            return;
        }

        isSaving = true;
        error = null;

        try
        {
            var startStr = formStartDate?.ToString("yyyy-MM-dd");
            var endStr   = formEndDate?.ToString("yyyy-MM-dd");

            ProjectResponse result;

            if (EditingProject is not null)
            {
                var request = new UpdateProjectRequest
                {
                    Dc = formDc, Status = formStatus, Name = formName,
                    Client = formClient, Type = formType,
                    StartDate = startStr, EndDate = endStr,
                    DeliveryManager = formDeliveryManager,
                    Renewal = formRenewal, RenewalObservation = formRenewalObservation
                };
                result = await ProjectsClient.UpdateAsync(EditingProject.Id, request, default);
            }
            else
            {
                var request = new CreateProjectRequest
                {
                    Dc = formDc, Status = formStatus, Name = formName,
                    Client = formClient, Type = formType,
                    StartDate = startStr, EndDate = endStr,
                    DeliveryManager = formDeliveryManager,
                    Renewal = formRenewal, RenewalObservation = formRenewalObservation
                };
                result = await ProjectsClient.CreateAsync(request, default);
            }

            await OnSaved.InvokeAsync(result);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
        finally
        {
            isSaving = false;
        }
    }
}
