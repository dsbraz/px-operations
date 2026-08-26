using Bunit;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps.Components;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

/// <summary>
/// F2: todo projeto da carteira aparece em exatamente uma coluna, e as colunas
/// seguem regra objetiva sobre dados que a API já entrega.
/// </summary>
public sealed class NpsCollectionBoardTests : TestContext
{
    [Fact]
    public void Every_project_should_land_in_exactly_one_column()
    {
        var cut = Render([
            Project(1, "Sem link", activeDispatches: 0),
            Project(2, "Aguardando", activeDispatches: 1, expiresInDays: 14),
            Project(3, "Recoleta", activeDispatches: 1, responses: 2, lastResponseDaysAgo: 60),
            Project(4, "Em dia", activeDispatches: 1, responses: 2, lastResponseDaysAgo: 3)
        ]);

        var counts = cut.FindAll(".kanban__count").Select(n => n.TextContent.Trim()).ToArray();
        Assert.Equal(["1", "1", "1", "1"], counts);
        Assert.Equal(4, cut.FindAll(".kcard").Count);
    }

    /// <summary>
    /// D7/D8: link expirado não vira coluna. É estado dentro de "Aguardando",
    /// no topo dela, e a ação troca de "Cobrar" para "Gerar novo link".
    /// </summary>
    [Fact]
    public void Expired_link_should_stay_in_waiting_at_the_top_with_a_new_link_action()
    {
        var cut = Render([
            Project(1, "Ainda vale", activeDispatches: 1, expiresInDays: 10),
            Project(2, "Venceu", activeDispatches: 1, expiresInDays: -3)
        ]);

        var waiting = cut.FindAll(".kanban__col")[1];
        var cards = waiting.QuerySelectorAll(".kcard");

        Assert.Equal(2, cards.Length);
        Assert.Contains("Venceu", cards[0].TextContent);
        Assert.Contains("is-expired", cards[0].ClassName);
        Assert.Contains("Link expirado há 3d", cards[0].TextContent);
        Assert.Contains("Gerar novo link", cards[0].TextContent);
        Assert.Contains("Cobrar", cards[1].TextContent);
    }

    /// <summary>
    /// F2: os dois temporais são deliberadamente diferentes. Só o prazo vira
    /// badge de alarme; a recência é sempre neutra, porque a coluna já carrega
    /// a urgência.
    /// </summary>
    [Fact]
    public void Only_the_deadline_becomes_an_alarm_badge()
    {
        var cut = Render([
            Project(1, "Apertado", activeDispatches: 1, expiresInDays: 2),
            Project(2, "Recoleta", activeDispatches: 1, responses: 1, lastResponseDaysAgo: 60)
        ]);

        var prazo = cut.FindAll(".kanban__col")[1].QuerySelector(".kcard__timing");
        Assert.Contains("is-warn", prazo!.ClassName);

        var recencia = cut.FindAll(".kanban__col")[2].QuerySelector(".kcard__timing");
        Assert.DoesNotContain("is-warn", recencia!.ClassName);
        Assert.DoesNotContain("is-danger", recencia.ClassName);
    }

    /// <summary>
    /// F2, critério de aceite: "prazo e recência são distinguíveis à primeira
    /// vista (ícone e tratamento diferentes)". Cor sozinha não cumpre — a
    /// recência é sempre neutra, então um prazo folgado e uma recência ficariam
    /// dois chips cinzas idênticos. O ícone é o que separa.
    /// </summary>
    [Fact]
    public void Deadline_and_recency_should_carry_different_icons()
    {
        var cut = Render([
            Project(1, "Folgado", activeDispatches: 1, expiresInDays: 14),
            Project(2, "Recoleta", activeDispatches: 1, responses: 1, lastResponseDaysAgo: 60)
        ]);

        var prazo = cut.FindAll(".kanban__col")[1].QuerySelector(".kcard__timing")!;
        var recencia = cut.FindAll(".kanban__col")[2].QuerySelector(".kcard__timing")!;

        Assert.Contains("kcard__timing--deadline", prazo.ClassName);
        Assert.Contains("kcard__timing--recency", recencia.ClassName);

        // Ambos neutros neste recorte: sem o ícone seriam indistinguíveis.
        Assert.DoesNotContain("is-warn", prazo.ClassName);
        Assert.DoesNotContain("is-warn", recencia.ClassName);
        Assert.NotEqual(
            prazo.QuerySelector("svg")!.InnerHtml,
            recencia.QuerySelector("svg")!.InnerHtml);
    }

    /// <summary>
    /// F6: a volta atrás é parte do fluxo — o card dispensado traz a ação de
    /// reativar nele mesmo, não escondida num menu.
    /// </summary>
    [Fact]
    public void Dismissed_card_should_offer_reactivation_on_the_card()
    {
        var cut = Render([Project(1, "Pausado", activeDispatches: 0, dismissed: "Cliente pediu pausa")]);

        var card = cut.Find(".kcard");
        Assert.Contains("is-dismissed", card.ClassName);
        Assert.Contains("Reativar coleta", card.TextContent);
        Assert.Contains("Cliente pediu pausa", card.TextContent);
    }

    /// <summary>
    /// F6: dispensar mora no menu "⋯" do card, como no protótipo. Reativar não
    /// fica lá — é ação do próprio card, para a volta atrás não ficar escondida.
    /// </summary>
    [Fact]
    public void Dismiss_should_live_in_the_card_menu_and_reactivate_should_not()
    {
        var cut = Render([
            Project(1, "Ativo", activeDispatches: 1, expiresInDays: 10),
            Project(2, "Pausado", activeDispatches: 0, dismissed: "Cliente pediu pausa")
        ]);

        var ativo = cut.FindAll(".kcard").Single(c => c.TextContent.Contains("Ativo"));
        var kebab = ativo.QuerySelector(".kcard__kebab");
        Assert.NotNull(kebab);
        kebab!.Click();
        Assert.Contains("Dispensar coleta", cut.Find(".kcard__menu").TextContent);

        // O card dispensado não oferece "dispensar" de novo, e traz a reativação
        // visível em vez de escondida no menu.
        var pausado = cut.FindAll(".kcard").Single(c => c.TextContent.Contains("Pausado"));
        Assert.Null(pausado.QuerySelector(".kcard__kebab"));
        Assert.Contains("Reativar coleta", pausado.TextContent);
    }

    private IRenderedComponent<NpsCollectionBoard> Render(IReadOnlyList<NpsProjectResponse> projects)
        => RenderComponent<NpsCollectionBoard>(p => p.Add(c => c.Projects, projects));

    private static NpsProjectResponse Project(
        int id, string name, int activeDispatches,
        int responses = 0, int? expiresInDays = null, int? lastResponseDaysAgo = null, string? dismissed = null)
        => new()
        {
            Id = id,
            Name = name,
            Client = "Cliente",
            Dc = "DC1",
            DeliveryManager = "Maria",
            ActiveDispatches = activeDispatches,
            ResponsesCount = responses,
            LastResponseAt = lastResponseDaysAgo is null
                ? null
                : DateTimeOffset.UtcNow.AddDays(-lastResponseDaysAgo.Value).ToString("O"),
            ActiveDispatchExpiresAt = expiresInDays is null
                ? null
                : DateTimeOffset.UtcNow.AddDays(expiresInDays.Value).ToString("O"),
            IsDismissed = dismissed is not null,
            DismissalReason = dismissed
        };
}
