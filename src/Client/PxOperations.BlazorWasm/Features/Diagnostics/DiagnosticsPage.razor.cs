using Microsoft.AspNetCore.Components;
using PxOperations.BlazorWasm.Api;

namespace PxOperations.BlazorWasm.Features.Diagnostics;

public partial class DiagnosticsPage : ComponentBase
{
    [Inject] private HealthClient HealthClient { get; set; } = default!;

    private bool isLoading = true;
    private string status = "Unknown";
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var readiness = await HealthClient.ReadyAsync(default);
            status = readiness?.Status ?? "Unknown";
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            isLoading = false;
        }
    }
}
