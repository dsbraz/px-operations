using Bunit;
using PxOperations.BlazorWasm.Features.ProjectHealth;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.ProjectHealth;

public sealed class ProjectHealthProjectsViewTests : BunitContext
{
    [Fact]
    public void Header_should_label_the_delivery_manager_as_the_responsible()
    {
        var cut = Render<ProjectHealthProjectsView>(parameters => parameters
            .Add(view => view.Entries, [ProjectHealthTestHelpers.MakeProjectHealth()]));

        var header = cut.Find(".ph-proj-head").TextContent;
        Assert.Contains("DC · Responsável", header);
        Assert.DoesNotContain("DM", header);
    }
}
