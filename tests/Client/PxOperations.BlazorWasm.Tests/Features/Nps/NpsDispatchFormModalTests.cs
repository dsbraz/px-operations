using Bunit;
using PxOperations.BlazorWasm.Features.Nps;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

public sealed class NpsDispatchFormModalTests : BunitContext
{
    public NpsDispatchFormModalTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Submit_should_be_blocked_while_the_dispatch_is_in_flight()
    {
        var cut = Render<NpsDispatchFormModal>(parameters => parameters
            .Add(modal => modal.Open, true)
            .Add(modal => modal.ProjectId, 1)
            .Add(modal => modal.IsSubmitting, true));

        Assert.True(cut.Find(".nps-create-submit").HasAttribute("disabled"));
    }

    [Fact]
    public void Submit_should_be_available_once_a_project_is_chosen()
    {
        var cut = Render<NpsDispatchFormModal>(parameters => parameters
            .Add(modal => modal.Open, true)
            .Add(modal => modal.ProjectId, 1));

        Assert.False(cut.Find(".nps-create-submit").HasAttribute("disabled"));
    }
}
