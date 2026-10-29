using Bunit;
using PxOperations.BlazorWasm.Features.Nps;
using PxOperations.BlazorWasm.Tests.Helpers;

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
    public void Submit_should_be_available_once_a_project_and_a_collector_are_chosen()
    {
        var cut = Render<NpsDispatchFormModal>(parameters => parameters
            .Add(modal => modal.Open, true)
            .Add(modal => modal.ProjectId, 1)
            .Add(modal => modal.CollectorName, "Julia Pereira"));

        Assert.False(cut.Find(".nps-create-submit").HasAttribute("disabled"));
    }

    [Fact]
    public void Submit_should_wait_for_the_collector()
    {
        var cut = Render<NpsDispatchFormModal>(parameters => parameters
            .Add(modal => modal.Open, true)
            .Add(modal => modal.ProjectId, 1));

        Assert.True(cut.Find(".nps-create-submit").HasAttribute("disabled"));
    }

    /// <summary>
    /// Quem gera o NPS pode ser de qualquer área, inclusive Sales, que não
    /// responde por projeto.
    /// </summary>
    [Fact]
    public void Collector_should_be_chosen_among_every_team_member()
    {
        string? reported = null;
        var cut = Render<NpsDispatchFormModal>(parameters => parameters
            .Add(modal => modal.Open, true)
            .Add(modal => modal.TeamMembers, TeamTestHelpers.TeamMembers())
            .Add(modal => modal.CollectorNameChanged, name => reported = name));

        var select = cut.Find(".nps-create-collector");
        Assert.Equal("Responsável pela coleta", select.ParentElement!.QuerySelector("span")!.TextContent.Trim());
        Assert.Equal(
            new[] { "", "Denise Santos", "Julia Pereira", "Pedro Vieira" },
            TeamTestHelpers.OptionValues(select));

        select.Change("Julia Pereira");

        Assert.Equal("Julia Pereira", reported);
    }
}
