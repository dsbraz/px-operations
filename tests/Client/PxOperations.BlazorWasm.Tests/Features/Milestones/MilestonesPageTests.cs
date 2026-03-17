using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Milestones;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Milestones;

public sealed class MilestonesPageTests : TestContext
{
    [Fact]
    public void Page_should_render_week_view_with_loaded_milestones()
    {
        var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
        var handler = new MilestonesTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(MilestonesTestHelpers.MilestonesJson(
            MilestonesTestHelpers.MakeMilestone(title: "Kickoff Alfa", date: today)));
        handler.AddResponse(ProjectsTestHelpers.ProjectsJson(
            ProjectsTestHelpers.MakeProject(id: 10, name: "Projeto A", dc: "DC1")));

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => httpClient);
        Services.AddScoped<MilestonesClient>();
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<MilestonesPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Kickoff Alfa", cut.Markup);
            Assert.Contains("Semana", cut.Markup);
        });
    }

    [Fact]
    public void Page_should_filter_by_search_term()
    {
        var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
        var handler = new MilestonesTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(MilestonesTestHelpers.MilestonesJson(
            MilestonesTestHelpers.MakeMilestone(id: 1, title: "Kickoff Alfa", projectName: "Projeto A", date: today),
            MilestonesTestHelpers.MakeMilestone(id: 2, title: "Entrega Beta", projectName: "Projeto B", date: today)));
        handler.AddResponse(ProjectsTestHelpers.ProjectsJson(
            ProjectsTestHelpers.MakeProject(id: 10, name: "Projeto A", dc: "DC1"),
            ProjectsTestHelpers.MakeProject(id: 11, name: "Projeto B", dc: "DC2")));

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => httpClient);
        Services.AddScoped<MilestonesClient>();
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<MilestonesPage>();

        cut.WaitForAssertion(() => Assert.Contains("Entrega Beta", cut.Markup));

        cut.Find("input[type=text]").Input("Alfa");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Kickoff Alfa", cut.Markup);
            Assert.DoesNotContain("Entrega Beta", cut.Markup);
        });
    }

    [Fact]
    public void Page_should_switch_to_calendar_view()
    {
        var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");
        var handler = new MilestonesTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(MilestonesTestHelpers.MilestonesJson(
            MilestonesTestHelpers.MakeMilestone(title: "Kickoff Alfa", date: today)));
        handler.AddResponse(ProjectsTestHelpers.ProjectsJson(
            ProjectsTestHelpers.MakeProject(id: 10, name: "Projeto A", dc: "DC1")));

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => httpClient);
        Services.AddScoped<MilestonesClient>();
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<MilestonesPage>();
        cut.WaitForAssertion(() => Assert.Contains("Mês", cut.Markup));

        cut.FindAll("button.vtab")[1].Click();

        cut.WaitForAssertion(() => Assert.Contains("cal-grid", cut.Markup));
    }
}
