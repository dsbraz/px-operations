using System.Net;
using System.Net.Http;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Projects;
using PxOperations.BlazorWasm.Tests.Helpers;

namespace PxOperations.BlazorWasm.Tests.Features.Projects;

/// <summary>
/// Testes isolados do componente <see cref="ProjectFormModal"/>.
/// O componente injeta <see cref="ProjectsClient"/> para chamadas Create/Update.
/// </summary>
public sealed class ProjectFormModalTests : TestContext
{
    [Fact]
    public void Should_render_create_form_with_novo_projeto_title()
    {
        Services.AddScoped(_ => ProjectsTestHelpers.CreateEmptyClient());
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<ProjectFormModal>();

        Assert.Contains("Novo Projeto", cut.Markup);
        Assert.Contains("overlay open", cut.Markup);
    }

    [Fact]
    public void Should_render_edit_form_prefilled_with_project_data()
    {
        Services.AddScoped(_ => ProjectsTestHelpers.CreateEmptyClient());
        Services.AddScoped<ProjectsClient>();

        var project = ProjectsTestHelpers.MakeProject(
            id: 1, dc: "DC2", status: "Programado", name: "Portal X",
            client: "Acme", type: "Escopo Fechado",
            startDate: "2026-03-01", endDate: "2026-09-01",
            deliveryManager: "Flavia de Castro",
            renewal: "Pendente", renewalObservation: "Obs test");

        var cut = RenderComponent<ProjectFormModal>(p => p
            .Add(c => c.EditingProject, project));

        Assert.Contains("Editar Projeto", cut.Markup);
        Assert.Contains("overlay open", cut.Markup);
        Assert.Contains("Portal X", cut.Markup);
    }

    [Fact]
    public void Should_fire_on_close_callback_when_cancel_is_clicked()
    {
        Services.AddScoped(_ => ProjectsTestHelpers.CreateEmptyClient());
        Services.AddScoped<ProjectsClient>();

        var closed = false;

        var cut = RenderComponent<ProjectFormModal>(p => p
            .Add(c => c.OnClose, Microsoft.AspNetCore.Components.EventCallback.Factory.Create(
                this, () => closed = true)));

        cut.Find(".mfoot .btn-ghost").Click();

        Assert.True(closed);
    }

    [Fact]
    public void Should_show_validation_error_when_name_is_empty_on_save()
    {
        Services.AddScoped(_ => ProjectsTestHelpers.CreateEmptyClient());
        Services.AddScoped<ProjectsClient>();

        var cut = RenderComponent<ProjectFormModal>();

        // Nome está vazio por padrão na criação; clicar Salvar deve exibir erro
        cut.Find(".mfoot .btn-purple").Click();

        Assert.Contains("Informe o nome do projeto", cut.Markup);
    }

    [Fact]
    public void Should_fire_on_saved_callback_after_successful_create()
    {
        var savedProject = ProjectsTestHelpers.MakeProject(id: 10, name: "Novo Portal");

        Services.AddScoped(_ => ProjectsTestHelpers.CreateClient(
            ProjectsTestHelpers.ProjectJson(savedProject), HttpStatusCode.Created));
        Services.AddScoped<ProjectsClient>();

        ProjectResponse? result = null;

        var cut = RenderComponent<ProjectFormModal>(p => p
            .Add(c => c.OnSaved, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<ProjectResponse>(
                this, r => result = r)));

        // Preenche o nome (campo obrigatório)
        cut.Find("input[type=text]").Change("Novo Portal");

        cut.Find(".mfoot .btn-purple").Click();

        cut.WaitForAssertion(() => Assert.NotNull(result));
    }
}
