using Bunit;
using PxOperations.BlazorWasm.Api;
using PxOperations.BlazorWasm.Features.Nps.Components;

namespace PxOperations.BlazorWasm.Tests.Features.Nps;

/// <summary>
/// F5: detalhe da coleta com links ativos rotulados por formato, prazo e estado
/// de proximidade. Critério de aceite: link a vencer mostra "expira em Xd"; link
/// vencido mostra "expirado há Xd" e a ação de gerar novo.
/// </summary>
public sealed class NpsCollectionDetailTests : TestContext
{
    [Fact]
    public void Link_about_to_expire_should_show_the_countdown_and_offer_copy()
    {
        var cut = Render(Dispatch(1, "Simplificado", expiresInDays: 3));

        var link = cut.Find(".detail-link");
        Assert.Contains("Simplificado", link.TextContent);
        Assert.Contains("Expira em 3d", link.TextContent);
        Assert.Contains("is-warn", link.ClassName);
        Assert.Contains("Copiar", link.TextContent);
        Assert.DoesNotContain("Gerar novo", link.TextContent);
    }

    [Fact]
    public void Expired_link_should_offer_a_new_one_instead_of_copy()
    {
        var cut = Render(Dispatch(1, "Completo", expiresInDays: -4));

        var link = cut.Find(".detail-link");
        Assert.Contains("Expirado há 4d", link.TextContent);
        Assert.Contains("is-expired", link.ClassName);
        Assert.Contains("Gerar novo", link.TextContent);
        Assert.DoesNotContain("Copiar", link.TextContent);
    }

    /// <summary>D3: o projeto pode ter dois links ativos, rotulados por formato.</summary>
    [Fact]
    public void Both_active_links_should_be_labelled_by_format()
    {
        var cut = Render(
            Dispatch(1, "Simplificado", expiresInDays: 10),
            Dispatch(2, "Completo", expiresInDays: 10));

        var formats = cut.FindAll(".detail-link__format").Select(n => n.TextContent.Trim()).ToArray();
        Assert.Equal(["Simplificado", "Completo"], formats);
    }

    /// <summary>
    /// "Links ATIVOS" é literal no F5: disparo fechado não compete por atenção
    /// no topo do detalhe.
    /// </summary>
    [Fact]
    public void Closed_dispatch_should_not_be_listed_as_active()
    {
        var fechado = Dispatch(2, "Completo", expiresInDays: 10);
        fechado.ClosedAt = "2026-08-01T00:00:00Z";
        var cut = Render(Dispatch(1, "Simplificado", expiresInDays: 10), fechado);

        Assert.Single(cut.FindAll(".detail-link"));
    }

    /// <summary>
    /// F5: o filtro por formato só aparece quando há os dois — com um formato só
    /// seria um controle que não filtra nada.
    /// </summary>
    [Fact]
    public void Format_filter_should_appear_only_when_both_formats_exist()
    {
        var soUm = Render(new[] { Dispatch(1, "Simplificado", 10) },
            [Response(9, "Simplificado"), Response(8, "Simplificado")]);
        Assert.Empty(soUm.FindAll(".detail-responses select"));

        var ambos = Render(new[] { Dispatch(1, "Simplificado", 10) },
            [Response(9, "Simplificado"), Response(8, "Completo")]);
        Assert.Single(ambos.FindAll(".detail-responses select"));
    }

    private IRenderedComponent<NpsCollectionDetail> Render(params NpsDispatchResponse[] dispatches)
        => Render(dispatches, []);

    private IRenderedComponent<NpsCollectionDetail> Render(
        IReadOnlyList<NpsDispatchResponse> dispatches, IReadOnlyList<NpsSurveyResponse> responses)
        => RenderComponent<NpsCollectionDetail>(p => p.Add(c => c.Detail, new NpsProjectDetailResponse
        {
            Project = new NpsProjectResponse
            {
                Id = 1, Name = "Projeto 1", Client = "Cliente", Dc = "DC1",
                DeliveryManager = "Maria", ResponsesCount = responses.Count
            },
            Contacts = [],
            Dispatches = [.. dispatches],
            RecentResponses = [.. responses]
        }));

    private static NpsDispatchResponse Dispatch(int id, string format, int expiresInDays)
        => new()
        {
            Id = id, ProjectId = 1, ProjectName = "Projeto 1",
            PeriodStart = "2026-08-01", PeriodEnd = "2026-08-31",
            Format = format, Language = "Português", Status = "Aberto",
            CreatedBy = "ops", CreatedAt = "2026-08-01T00:00:00Z",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(expiresInDays).ToString("O"),
            IsExpired = expiresInDays <= 0,
            TargetsCount = 1, ResponsesCount = 0
        };

    private static NpsSurveyResponse Response(int score, string format)
        => new()
        {
            Id = score, ProjectId = 1, ProjectName = "Projeto 1", DispatchId = 1, TargetId = 1,
            Score = score, Classification = "Promotor", Format = format,
            SubmittedAt = "2026-08-20T00:00:00Z"
        };
}
