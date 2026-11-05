using Bunit;
using PxOperations.BlazorWasm.Features.Team;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Team;

public sealed class TeamMemberOptionsTests : BunitContext
{
    [Fact]
    public void Options_should_list_every_member_together_in_alphabetical_order()
    {
        var cut = Render(builder =>
        {
            builder.OpenElement(0, "select");
            builder.OpenComponent<TeamMemberOptions>(1);
            builder.AddComponentParameter(2, nameof(TeamMemberOptions.Members), TeamTestHelpers.TeamMembers());
            builder.CloseComponent();
            builder.CloseElement();
        });

        Assert.Empty(cut.FindAll("optgroup"));
        Assert.Equal(
            new[] { "Denise Santos", "Julia Pereira", "Pedro Vieira" },
            TeamTestHelpers.OptionValues(cut.Find("select")));
    }
}
