using System.Net;
using System.Net.Http;
using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Projects;

namespace PxOperations.BlazorWasm.Tests;

public sealed class ProjectsPageTests : TestContext
{
    // ── LIST ────────────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_render_table_with_projects()
    {
        Services.AddScoped(_ => CreateClient("""
            [{"id":1,"dc":"DC1","status":"Em andamento","name":"Alpha","client":"Client A","type":"Squad","startDate":"2026-01-01","endDate":"2026-12-31","deliveryManager":"John","renewal":"None","renewalObservation":null}]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Alpha", component.Markup);
            Assert.Contains("DC1", component.Markup);
            Assert.Contains("Client A", component.Markup);
        });
    }

    [Fact]
    public void Page_should_render_empty_state()
    {
        Services.AddScoped(_ => CreateClient("[]", HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Nenhum projeto encontrado", component.Markup);
        });
    }

    [Fact]
    public void Page_should_show_loading()
    {
        Services.AddScoped(_ => new HttpClient(new DelayedHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost")
        });
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        Assert.Contains("Carregando", component.Markup);
    }

    // ── STATS BAR ───────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_render_stats_bar_with_project_count()
    {
        Services.AddScoped(_ => CreateClient("""
            [
              {"id":1,"dc":"DC1","status":"Em andamento","name":"Alpha","client":"CPFL","type":"Squad","startDate":"2026-01-01","endDate":"2026-12-31","deliveryManager":null,"renewal":"None","renewalObservation":null},
              {"id":2,"dc":"DC2","status":"Encerrado","name":"Beta","client":"CPFL","type":"Squad","startDate":"2025-01-01","endDate":"2025-06-30","deliveryManager":null,"renewal":"Aprovada","renewalObservation":null}
            ]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Projetos", component.Markup);
            Assert.Contains("Em andamento", component.Markup);
        });
    }

    // ── MODAL ───────────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_show_create_modal_when_button_clicked()
    {
        Services.AddScoped(_ => CreateClient("[]", HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() => Assert.DoesNotContain("overlay open", component.Markup));

        component.Find("button.btn-purple").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Novo Projeto", component.Markup);
            Assert.Contains("overlay open", component.Markup);
        });
    }

    [Fact]
    public void Page_should_open_edit_modal_with_project_data()
    {
        Services.AddScoped(_ => CreateClient("""
            [{"id":1,"dc":"DC2","status":"Programado","name":"Portal X","client":"Acme","type":"Escopo Fechado","startDate":"2026-03-01","endDate":"2026-09-01","deliveryManager":"Flavia de Castro","renewal":"Pendente","renewalObservation":"Obs test"}]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() => Assert.Contains("Portal X", component.Markup));

        component.Find("button.ibtn:not(.del)").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Editar Projeto", component.Markup);
            Assert.Contains("overlay open", component.Markup);
            Assert.Contains("Portal X", component.Markup);
        });
    }

    [Fact]
    public void Page_should_close_modal_on_cancel()
    {
        Services.AddScoped(_ => CreateClient("[]", HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.Find("button.btn-purple").Click();
        component.WaitForAssertion(() => Assert.Contains("overlay open", component.Markup));

        // click the Cancel button inside the modal footer
        component.Find(".mfoot .btn-ghost").Click();

        component.WaitForAssertion(() => Assert.DoesNotContain("overlay open", component.Markup));
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_delete_project_and_remove_from_table()
    {
        var handler = new MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, """
            [{"id":1,"dc":"DC1","status":"Em andamento","name":"ToDelete","client":"C","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null}]
            """, HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Delete, "", HttpStatusCode.NoContent);

        Services.AddScoped(_ => new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() => Assert.Contains("ToDelete", component.Markup));

        component.Find("button.ibtn.del").Click();

        component.WaitForAssertion(() => Assert.DoesNotContain("ToDelete", component.Markup));
    }

    // ── WEEKLY PULSE ────────────────────────────────────────────────────────

    [Fact]
    public void Pulse_should_render_four_cards_with_labels()
    {
        Services.AddScoped(_ => CreateClient("[]", HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Novos programados", component.Markup);
            Assert.Contains("Iniciados semana ant.", component.Markup);
            Assert.Contains("Encerrados semana ant.", component.Markup);
            Assert.Contains("Renova", component.Markup); // "Renovações aprovadas"
        });
    }

    [Fact]
    public void Pulse_should_count_programmed_projects_in_new_card()
    {
        Services.AddScoped(_ => CreateClient("""
            [
              {"id":1,"dc":"DC1","status":"Programado","name":"Novo Projeto","client":"C","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null},
              {"id":2,"dc":"DC1","status":"Em andamento","name":"Ativo","client":"C","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null}
            ]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() =>
        {
            // pulse body is open by default; the "Novos programados" card count should be 1
            var pulseCounts = component.FindAll(".pc-new .pc-count");
            Assert.Single(pulseCounts);
            Assert.Equal("1", pulseCounts[0].TextContent.Trim());
        });
    }

    [Fact]
    public void Pulse_should_count_approved_renewals()
    {
        Services.AddScoped(_ => CreateClient("""
            [
              {"id":1,"dc":"DC1","status":"Em andamento","name":"Portal X","client":"C","type":"Squad","startDate":null,"endDate":"2026-12-31","deliveryManager":null,"renewal":"Aprovada","renewalObservation":null},
              {"id":2,"dc":"DC1","status":"Em andamento","name":"Portal Y","client":"C","type":"Squad","startDate":null,"endDate":"2026-12-31","deliveryManager":null,"renewal":"Pendente","renewalObservation":null}
            ]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() =>
        {
            var renewCount = component.FindAll(".pc-renew .pc-count");
            Assert.Single(renewCount);
            Assert.Equal("1", renewCount[0].TextContent.Trim());
        });
    }

    [Fact]
    public void Pulse_should_show_project_name_in_card_items()
    {
        Services.AddScoped(_ => CreateClient("""
            [{"id":1,"dc":"DC1","status":"Programado","name":"Meu Projeto Novo","client":"C","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null}]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Meu Projeto Novo", component.Markup);
        });
    }

    [Fact]
    public void Pulse_should_collapse_and_expand_on_header_click()
    {
        Services.AddScoped(_ => CreateClient("[]", HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() => Assert.Contains("pulse-body open", component.Markup));

        // click header to collapse
        component.Find(".pulse-header").Click();

        component.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("pulse-body open", component.Markup);
            Assert.Contains("pulse-header collapsed", component.Markup);
        });

        // click again to expand
        component.Find(".pulse-header").Click();

        component.WaitForAssertion(() => Assert.Contains("pulse-body open", component.Markup));
    }

    [Fact]
    public void Pulse_should_show_empty_message_when_no_projects_in_category()
    {
        Services.AddScoped(_ => CreateClient("[]", HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Nenhum esta semana", component.Markup);
            Assert.Contains("Nenhuma aprovada", component.Markup);
        });
    }

    // ── RENOVAÇÕES VIEW ──────────────────────────────────────────────────────

    [Fact]
    public void RenovacoesTab_should_show_indicator_with_computed_values()
    {
        Services.AddScoped(_ => CreateClient("""
            [
              {"id":1,"dc":"DC1","status":"Em andamento","name":"Portal A","client":"C","type":"Squad","startDate":"2026-01-01","endDate":"2026-12-31","deliveryManager":"DM1","renewal":"Aprovada","renewalObservation":null},
              {"id":2,"dc":"DC1","status":"Em andamento","name":"Portal B","client":"C","type":"Squad","startDate":"2026-01-01","endDate":"2026-12-31","deliveryManager":"DM1","renewal":"Pendente","renewalObservation":null},
              {"id":3,"dc":"DC1","status":"Em andamento","name":"Portal C","client":"C","type":"Squad","startDate":"2026-01-01","endDate":"2026-12-31","deliveryManager":"DM1","renewal":"None","renewalObservation":null}
            ]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        // switch to Renovações tab (index 2: Lista=0, Kanban=1, Renovações=2)
        component.WaitForAssertion(() => Assert.Contains("Portal A", component.Markup));
        component.FindAll("button.vtab")[2].Click();

        component.WaitForAssertion(() =>
        {
            // indicator should show percentage
            Assert.Contains("ri-pct", component.Markup);
            // 2 out of 3 have status => 66%
            Assert.Contains("66%", component.Markup);
            // breakdown numbers
            Assert.Contains("ri-num aprov", component.Markup);
        });
    }

    [Fact]
    public void RenovacoesTab_should_show_dc_bars_when_all_dcs_selected()
    {
        Services.AddScoped(_ => CreateClient("""
            [
              {"id":1,"dc":"DC1","status":"Em andamento","name":"P1","client":"C","type":"Squad","startDate":"2026-01-01","endDate":"2026-06-30","deliveryManager":null,"renewal":"Aprovada","renewalObservation":null},
              {"id":2,"dc":"DC2","status":"Em andamento","name":"P2","client":"C","type":"Squad","startDate":"2026-01-01","endDate":"2026-06-30","deliveryManager":null,"renewal":"Pendente","renewalObservation":null}
            ]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() => Assert.Contains("P1", component.Markup));
        component.FindAll("button.vtab")[2].Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("dc-bars-grid", component.Markup);
            Assert.Contains("dc-bar-card", component.Markup);
            Assert.Contains("DC1", component.Markup);
            Assert.Contains("DC2", component.Markup);
        });
    }

    [Fact]
    public void RenovacoesTab_should_show_project_cards_for_renewals()
    {
        Services.AddScoped(_ => CreateClient("""
            [
              {"id":1,"dc":"DC3","status":"Em andamento","name":"Projeto Renovando","client":"Acme","type":"Squad","startDate":"2026-01-01","endDate":"2026-09-30","deliveryManager":"Ana","renewal":"Em andamento","renewalObservation":"Negociação em curso"}
            ]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() => Assert.Contains("Projeto Renovando", component.Markup));
        component.FindAll("button.vtab")[2].Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("proj-card highlighted", component.Markup);
            Assert.Contains("Projeto Renovando", component.Markup);
            Assert.Contains("Acme", component.Markup);
            Assert.Contains("Negociação em curso", component.Markup);
        });
    }

    [Fact]
    public void RenovacoesTab_should_show_empty_state_when_no_renewals_in_period()
    {
        Services.AddScoped(_ => CreateClient("""
            [{"id":1,"dc":"DC1","status":"Em andamento","name":"Sem Renov","client":"C","type":"Squad","startDate":"2026-01-01","endDate":"2026-12-31","deliveryManager":null,"renewal":"None","renewalObservation":null}]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() => Assert.Contains("Sem Renov", component.Markup));
        component.FindAll("button.vtab")[2].Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Nenhuma renova", component.Markup);
        });
    }

    // ── FILTERING ───────────────────────────────────────────────────────────

    [Fact]
    public void Page_should_filter_projects_by_search_term()
    {
        Services.AddScoped(_ => CreateClient("""
            [
              {"id":1,"dc":"DC1","status":"Em andamento","name":"Alpha Project","client":"CPFL","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null},
              {"id":2,"dc":"DC2","status":"Em andamento","name":"Beta Project","client":"Alelo","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null}
            ]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Alpha Project", component.Markup);
            Assert.Contains("Beta Project", component.Markup);
        });

        component.Find("input[type=text]").Input("Alpha");

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Alpha Project", component.Markup);
            Assert.DoesNotContain("Beta Project", component.Markup);
        });
    }

    // ── KANBAN VIEW ─────────────────────────────────────────────────────────

    [Fact]
    public void KanbanTab_should_render_board_grouped_by_status_by_default()
    {
        Services.AddScoped(_ => CreateClient("""
            [
              {"id":1,"dc":"DC1","status":"Em andamento","name":"Projeto Ativo","client":"C","type":"Squad","startDate":"2026-01-01","endDate":"2026-12-31","deliveryManager":"DM1","renewal":"None","renewalObservation":null},
              {"id":2,"dc":"DC1","status":"Programado","name":"Projeto Futuro","client":"C","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null},
              {"id":3,"dc":"DC1","status":"Encerrado","name":"Projeto Velho","client":"C","type":"Squad","startDate":"2024-01-01","endDate":"2025-01-01","deliveryManager":null,"renewal":"None","renewalObservation":null}
            ]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();
        component.WaitForAssertion(() => Assert.Contains("Projeto Ativo", component.Markup));

        component.FindAll("button.vtab")[1].Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("kanban-board", component.Markup);
            Assert.Equal(3, component.FindAll(".kanban-column").Count);
            Assert.Contains("Programado", component.Markup);
            Assert.Contains("Em andamento", component.Markup);
            Assert.Contains("Encerrado", component.Markup);
        });
    }

    [Fact]
    public void KanbanTab_should_show_full_card_details()
    {
        Services.AddScoped(_ => CreateClient("""
            [{"id":1,"dc":"DC3","status":"Em andamento","name":"Portal X","client":"Acme","type":"Escopo Fechado","startDate":"2026-01-01","endDate":"2026-12-31","deliveryManager":"Ana Souza","renewal":"Pendente","renewalObservation":null}]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();
        component.WaitForAssertion(() => Assert.Contains("Portal X", component.Markup));

        component.FindAll("button.vtab")[1].Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("kanban-card", component.Markup);
            Assert.Contains("Portal X", component.Markup);
            Assert.Contains("DC3", component.Markup);
            Assert.Contains("Acme", component.Markup);
            Assert.Contains("Escopo Fechado", component.Markup);
            Assert.Contains("Ana Souza", component.Markup);
        });
    }

    [Fact]
    public void KanbanTab_should_group_by_renewal_showing_four_columns()
    {
        Services.AddScoped(_ => CreateClient("""
            [
              {"id":1,"dc":"DC1","status":"Em andamento","name":"P1","client":"C","type":"Squad","startDate":null,"endDate":"2026-12-31","deliveryManager":null,"renewal":"Aprovada","renewalObservation":null},
              {"id":2,"dc":"DC1","status":"Em andamento","name":"P2","client":"C","type":"Squad","startDate":null,"endDate":"2026-12-31","deliveryManager":null,"renewal":"Pendente","renewalObservation":null}
            ]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();
        component.WaitForAssertion(() => Assert.Contains("P1", component.Markup));

        component.FindAll("button.vtab")[1].Click();
        component.WaitForAssertion(() => Assert.Contains("kanban-board", component.Markup));

        component.FindAll("button.kgtab")[1].Click(); // pill: 0=Status, 1=Renovação, 2=DC

        component.WaitForAssertion(() =>
        {
            Assert.Equal(4, component.FindAll(".kanban-column").Count);
            Assert.Contains("Aprovada", component.Markup);
            Assert.Contains("Pendente", component.Markup);
            Assert.Contains("Sem status", component.Markup);
        });
    }

    [Fact]
    public void KanbanTab_should_group_by_dc_showing_six_columns()
    {
        Services.AddScoped(_ => CreateClient("""
            [
              {"id":1,"dc":"DC1","status":"Em andamento","name":"P1","client":"C","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null},
              {"id":2,"dc":"DC3","status":"Em andamento","name":"P2","client":"C","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null}
            ]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();
        component.WaitForAssertion(() => Assert.Contains("P1", component.Markup));

        component.FindAll("button.vtab")[1].Click();
        component.WaitForAssertion(() => Assert.Contains("kanban-board", component.Markup));

        component.FindAll("button.kgtab")[2].Click(); // pill: 0=Status, 1=Renovação, 2=DC

        component.WaitForAssertion(() =>
        {
            Assert.Equal(6, component.FindAll(".kanban-column").Count);
        });
    }

    [Fact]
    public void KanbanTab_should_respect_search_filter()
    {
        Services.AddScoped(_ => CreateClient("""
            [
              {"id":1,"dc":"DC1","status":"Em andamento","name":"Alpha Squad","client":"C","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null},
              {"id":2,"dc":"DC1","status":"Em andamento","name":"Beta Squad","client":"C","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null}
            ]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();
        component.WaitForAssertion(() => Assert.Contains("Alpha Squad", component.Markup));

        component.Find("input[type=text]").Input("Alpha");
        component.FindAll("button.vtab")[1].Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Alpha Squad", component.Markup);
            Assert.DoesNotContain("Beta Squad", component.Markup);
        });
    }

    [Fact]
    public void KanbanTab_card_click_should_open_edit_modal()
    {
        Services.AddScoped(_ => CreateClient("""
            [{"id":1,"dc":"DC1","status":"Em andamento","name":"Portal Editavel","client":"C","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null}]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();
        component.WaitForAssertion(() => Assert.Contains("Portal Editavel", component.Markup));

        component.FindAll("button.vtab")[1].Click();
        component.WaitForAssertion(() => Assert.Contains("kanban-card", component.Markup));

        component.Find(".kanban-card").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("overlay open", component.Markup);
            Assert.Contains("Editar Projeto", component.Markup);
        });
    }

    [Fact]
    public void KanbanTab_drag_drop_should_update_project_status_and_show_toast()
    {
        var handler = new MultiStubHttpMessageHandler();
        handler.AddResponse(HttpMethod.Get, """
            [{"id":1,"dc":"DC1","status":"Programado","name":"Mover Projeto","client":"C","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null}]
            """, HttpStatusCode.OK);
        handler.AddResponse(HttpMethod.Patch, """
            {"id":1,"dc":"DC1","status":"Em andamento","name":"Mover Projeto","client":"C","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null}
            """, HttpStatusCode.OK);

        Services.AddScoped(_ => new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();
        component.WaitForAssertion(() => Assert.Contains("Mover Projeto", component.Markup));

        component.FindAll("button.vtab")[1].Click();
        component.WaitForAssertion(() => Assert.Contains("kanban-card", component.Markup));

        // drag from "Programado" column card to "Em andamento" column body
        component.Find(".kanban-card").TriggerEvent("ondragstart", new DragEventArgs());
        component.FindAll(".kanban-col-body")[1].TriggerEvent("ondrop", new DragEventArgs());

        component.WaitForAssertion(() =>
        {
            Assert.Contains("Projeto movido", component.Markup);
        });
    }

    [Fact]
    public void KanbanTab_empty_column_should_show_empty_message()
    {
        Services.AddScoped(_ => CreateClient("""
            [{"id":1,"dc":"DC1","status":"Em andamento","name":"Unico","client":"C","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null}]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();
        component.WaitForAssertion(() => Assert.Contains("Unico", component.Markup));

        component.FindAll("button.vtab")[1].Click();

        component.WaitForAssertion(() =>
        {
            // Programado and Encerrado columns are empty
            Assert.Contains("Nenhum projeto", component.Markup);
        });
    }

    // ── TABLE COLLAPSE ──────────────────────────────────────────────────────

    [Fact]
    public void ListTab_table_should_collapse_and_expand_on_minimize_button_click()
    {
        Services.AddScoped(_ => CreateClient("""
            [{"id":1,"dc":"DC1","status":"Em andamento","name":"Alpha","client":"C","type":"Squad","startDate":null,"endDate":null,"deliveryManager":null,"renewal":"None","renewalObservation":null}]
            """, HttpStatusCode.OK));
        Services.AddScoped<ProjectsClient>();

        var component = RenderComponent<ProjectsPage>();

        component.WaitForAssertion(() => Assert.Contains("Alpha", component.Markup));

        // table is visible by default
        component.WaitForAssertion(() => Assert.Contains("table-wrap open", component.Markup));

        // click the minimize button
        component.Find("button.collapse-btn").Click();

        component.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("table-wrap open", component.Markup);
            Assert.Contains("Expandir lista", component.Markup);
        });

        // click again to expand
        component.Find("button.collapse-btn").Click();

        component.WaitForAssertion(() =>
        {
            Assert.Contains("table-wrap open", component.Markup);
            Assert.Contains("Minimizar lista", component.Markup);
        });
    }

    // ── HELPERS ─────────────────────────────────────────────────────────────

    private static HttpClient CreateClient(string content, HttpStatusCode statusCode)
    {
        return new HttpClient(new StubHttpMessageHandler(content, statusCode))
        {
            BaseAddress = new Uri("http://localhost")
        };
    }

    private sealed class StubHttpMessageHandler(string content, HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private sealed class MultiStubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpMethod Method, string Content, HttpStatusCode Status)> _responses = new();

        public void AddResponse(HttpMethod method, string content, HttpStatusCode status)
            => _responses.Enqueue((method, content, status));

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_responses.TryDequeue(out var r))
            {
                var response = new HttpResponseMessage(r.Status)
                {
                    Content = new StringContent(r.Content, Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class DelayedHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
