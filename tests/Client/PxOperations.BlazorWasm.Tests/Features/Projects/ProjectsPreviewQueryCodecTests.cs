using PxOperations.BlazorWasm.Features.Projects.Preview;

namespace PxOperations.BlazorWasm.Tests.Features.Projects;

public sealed class ProjectsPreviewQueryCodecTests
{
    [Fact]
    public void Parse_should_accept_repeated_values_and_ignore_unknown_options()
    {
        var state = ProjectsPreviewQueryCodec.Parse(
            "?dc=DC2&dc=DC1&dc=UNKNOWN&status=Em%20andamento&type=Squad&q=cloud");

        Assert.Equal("cloud", state.Search);
        Assert.Equal(["DC1", "DC2"], state.DeliveryCenters);
        Assert.Equal(["Em andamento"], state.Statuses);
        Assert.Equal(["Squad"], state.Types);
        Assert.Empty(state.Renewals);
    }

    [Fact]
    public void Build_should_be_deterministic_and_use_repeated_parameters()
    {
        var state = new ProjectsPreviewFilterState(
            Search: "core banking",
            DeliveryCenters: ["DC2", "DC1"],
            Statuses: ["Programado", "Em andamento"],
            Types: [],
            Renewals: ["Aprovada"]);

        var uri = ProjectsPreviewQueryCodec.Build(state);

        Assert.Equal(
            "/preview/projects?q=core%20banking&dc=DC1&dc=DC2&status=Em%20andamento&status=Programado&renewal=Aprovada",
            uri);
    }

    [Fact]
    public void Build_should_omit_defaults()
    {
        var uri = ProjectsPreviewQueryCodec.Build(ProjectsPreviewFilterState.Empty);

        Assert.Equal("/preview/projects", uri);
    }
}
