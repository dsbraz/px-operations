using System.Net;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Milestones;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Milestones;

public sealed class MilestonesPageTests : BunitContext
{
    // A grade da semana renderiza segunda a sexta, então um marco datado de
    // sábado ou domingo simplesmente não aparece: ancorados em DateTime.Today,
    // estes testes quebravam todo fim de semana. A segunda-feira da semana
    // corrente é sempre visível e mantém o teste independente do dia em que roda.
    private static string MondayOfCurrentWeek()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var offset = (7 + ((int)today.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
        return today.AddDays(-offset).ToString("yyyy-MM-dd");
    }

    [Fact]
    public void Page_should_render_week_view_with_loaded_milestones()
    {
        var today = MondayOfCurrentWeek();
        var handler = new MilestonesTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(MilestonesTestHelpers.MilestonesJson(
            MilestonesTestHelpers.MakeMilestone(title: "Kickoff Alfa", date: today)));
        handler.AddResponse(ProjectsTestHelpers.ProjectsJson(
            ProjectsTestHelpers.MakeProject(id: 10, name: "Projeto A", dc: "DC1")));

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => httpClient);
        Services.AddScoped<MilestonesClient>();
        Services.AddScoped<ProjectsClient>();

        var cut = Render<MilestonesPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Kickoff Alfa", cut.Markup);
            Assert.Contains("Semana", cut.Markup);
        });
    }

    [Fact]
    public void Page_should_filter_by_search_term()
    {
        var today = MondayOfCurrentWeek();
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

        var cut = Render<MilestonesPage>();

        cut.WaitForAssertion(() => Assert.Contains("Entrega Beta", cut.Markup));

        cut.Find("input[type=text]").Input("Alfa");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Kickoff Alfa", cut.Markup);
            Assert.DoesNotContain("Entrega Beta", cut.Markup);
        });
    }

    [Fact]
    public void Page_should_render_project_type_filter_and_filter_by_linked_project_type()
    {
        var today = MondayOfCurrentWeek();
        var handler = new MilestonesTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(MilestonesTestHelpers.MilestonesJson(
            MilestonesTestHelpers.MakeMilestone(id: 1, projectId: 10, title: "Marco Squad", projectName: "Projeto A", date: today),
            MilestonesTestHelpers.MakeMilestone(id: 2, projectId: 11, title: "Marco Escopo", projectName: "Projeto B", date: today)));
        handler.AddResponse(ProjectsTestHelpers.ProjectsJson(
            ProjectsTestHelpers.MakeProject(id: 10, name: "Projeto A", type: "Squad"),
            ProjectsTestHelpers.MakeProject(id: 11, name: "Projeto B", type: "Escopo Fechado")));

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => httpClient);
        Services.AddScoped<MilestonesClient>();
        Services.AddScoped<ProjectsClient>();

        var cut = Render<MilestonesPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Tipo de Projeto", cut.Markup);
            Assert.Contains("Marco Squad", cut.Markup);
            Assert.Contains("Marco Escopo", cut.Markup);
        });

        cut.FindAll("select")[1].Change("Escopo Fechado");

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Marco Squad", cut.Markup);
            Assert.Contains("Marco Escopo", cut.Markup);
        });
    }

    [Fact]
    public void Page_should_switch_to_calendar_view()
    {
        var today = MondayOfCurrentWeek();
        var handler = new MilestonesTestHelpers.MultiStubHttpMessageHandler();
        handler.AddResponse(MilestonesTestHelpers.MilestonesJson(
            MilestonesTestHelpers.MakeMilestone(title: "Kickoff Alfa", date: today)));
        handler.AddResponse(ProjectsTestHelpers.ProjectsJson(
            ProjectsTestHelpers.MakeProject(id: 10, name: "Projeto A", dc: "DC1")));

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        Services.AddScoped(_ => httpClient);
        Services.AddScoped<MilestonesClient>();
        Services.AddScoped<ProjectsClient>();

        var cut = Render<MilestonesPage>();
        cut.WaitForAssertion(() => Assert.Contains("Mês", cut.Markup));

        cut.FindAll("button.vtab")[1].Click();

        cut.WaitForAssertion(() => Assert.Contains("cal-grid", cut.Markup));
    }
}
