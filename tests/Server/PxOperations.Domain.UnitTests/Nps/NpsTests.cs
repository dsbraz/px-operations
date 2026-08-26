using PxOperations.Domain.Exceptions;
using PxOperations.Domain.Nps;
using PxOperations.Domain.Nps.Calculation;

namespace PxOperations.Domain.UnitTests.Nps;

public sealed class NpsTests
{
    [Theory]
    [InlineData(0, NpsClassification.Detractor)]
    [InlineData(6, NpsClassification.Detractor)]
    [InlineData(7, NpsClassification.Passive)]
    [InlineData(8, NpsClassification.Passive)]
    [InlineData(9, NpsClassification.Promoter)]
    [InlineData(10, NpsClassification.Promoter)]
    public void Classify_should_follow_nps_score_bands(int score, NpsClassification expected)
    {
        Assert.Equal(expected, NpsCalculator.Classify(score));
    }

    [Fact]
    public void CalculateOfficialScore_should_return_promoters_percentage_minus_detractors_percentage()
    {
        var score = NpsCalculator.CalculateOfficialScore([
            NpsClassification.Promoter,
            NpsClassification.Promoter,
            NpsClassification.Passive,
            NpsClassification.Detractor
        ]);

        Assert.Equal(25.0m, score);
    }

    [Fact]
    public void Simplified_response_should_keep_dimensions_and_tags_null_when_submitted_that_way()
    {
        var response = SurveyResponse.Submit(
            projectId: 1,
            dispatchId: 2,
            targetId: 3,
            contactId: null,
            score: 10,
            businessValue: null,
            schedule: null,
            quality: null,
            communication: null,
            tags: null,
            comment: "Great",
            respondentName: "Jane",
            respondentEmail: "jane@example.com",
            now: DateTimeOffset.UtcNow);

        Assert.Null(response.BusinessValue);
        Assert.Null(response.Schedule);
        Assert.Null(response.Quality);
        Assert.Null(response.Communication);
        Assert.Null(response.Tags);
        Assert.Equal(NpsClassification.Promoter, response.Classification);
    }

    /// <summary>
    /// D10: a nota de recomendação passa a ser 1 a 10. O zero deixa de ser
    /// aceito na entrada — as respostas antigas em 0 a 10 continuam válidas no
    /// banco, e é isso que a decisão de segregar preserva.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void Response_should_reject_score_outside_nps_range(int score)
    {
        Assert.Throws<BusinessRuleValidationException>(() => SurveyResponse.Submit(
            1, 2, 3, null, score, null, null, null, null, null, null, null, null, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(10)]
    public void Response_should_accept_score_inside_nps_range(int score)
    {
        var response = SurveyResponse.Submit(
            1, 2, 3, null, score, null, null, null, null, null, null, null, null, DateTimeOffset.UtcNow);

        Assert.Equal(score, response.Score);
    }

    /// <summary>
    /// D10: aspectos da entrega vão de 1 a 5. Escala curta reduz o esforço nos
    /// quatro. Nulo segue válido — é o formato Simplificado, que não os coleta.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Response_should_reject_aspect_outside_range(int aspect)
    {
        Assert.Throws<BusinessRuleValidationException>(() => SurveyResponse.Submit(
            1, 2, 3, null, 9, aspect, null, null, null, null, null, null, null, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// D10 vale para os quatro aspectos, não só o primeiro.
    /// </summary>
    [Fact]
    public void Response_should_reject_out_of_range_in_any_aspect()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<BusinessRuleValidationException>(() => SurveyResponse.Submit(
            1, 2, 3, null, 9, null, 6, null, null, null, null, null, null, now));
        Assert.Throws<BusinessRuleValidationException>(() => SurveyResponse.Submit(
            1, 2, 3, null, 9, null, null, 0, null, null, null, null, null, now));
        Assert.Throws<BusinessRuleValidationException>(() => SurveyResponse.Submit(
            1, 2, 3, null, 9, null, null, null, 6, null, null, null, null, now));
    }

    /// <summary>
    /// B13: o quarto aspecto é "valor gerado para o negócio". Scope fica só
    /// para o histórico e nunca mais é gravado — é assim que as duas séries
    /// ficam distinguíveis sem perder nenhuma delas.
    /// </summary>
    [Fact]
    public void Complete_response_should_store_business_value_and_never_scope()
    {
        var response = SurveyResponse.Submit(
            1, 2, 3, null, 9, 5, 4, 3, 2, null, null, null, null, DateTimeOffset.UtcNow);

        Assert.Equal(5, response.BusinessValue);
        Assert.Null(response.Scope);
    }

    /// <summary>
    /// D10 move a régua: detrator passa a ser 1 a 6, e não mais 0 a 6.
    /// </summary>
    [Theory]
    [InlineData(1, NpsClassification.Detractor)]
    [InlineData(6, NpsClassification.Detractor)]
    [InlineData(7, NpsClassification.Passive)]
    [InlineData(8, NpsClassification.Passive)]
    [InlineData(9, NpsClassification.Promoter)]
    [InlineData(10, NpsClassification.Promoter)]
    public void Classify_should_follow_the_new_ruler(int score, NpsClassification expected)
    {
        Assert.Equal(expected, NpsCalculator.Classify(score));
    }

    /// <summary>
    /// B12/D7: o link vale 20 dias contados da geração. O prazo aparece no card,
    /// no detalhe e no próprio formulário, e vencido não aceita mais resposta.
    /// </summary>
    [Fact]
    public void Dispatch_should_expire_twenty_days_after_creation()
    {
        var now = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        var dispatch = Dispatch.Create(
            projectId: 1,
            periodStart: new DateOnly(2026, 8, 1),
            periodEnd: new DateOnly(2026, 8, 31),
            format: NpsFormFormat.Simplified,
            language: NpsLanguage.Portuguese,
            createdBy: "ops",
            now: now);

        Assert.Equal(now.AddDays(20), dispatch.ExpiresAt);
        Assert.False(dispatch.IsExpired(now.AddDays(19)));
        // No instante exato do vencimento já não vale: o prazo é fechado no fim.
        Assert.True(dispatch.IsExpired(now.AddDays(20)));
    }

    /// <summary>
    /// F6: dispensar exige motivo e é reversível sem perder o histórico.
    /// </summary>
    [Fact]
    public void Collection_waiver_should_require_a_reason_and_be_reversible()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<BusinessRuleValidationException>(
            () => CollectionWaiver.Dismiss(1, "   ", now));

        var waiver = CollectionWaiver.Dismiss(1, "Cliente pediu pausa", now);
        Assert.True(waiver.IsActive);
        Assert.Equal("Cliente pediu pausa", waiver.Reason);

        waiver.Reactivate(now.AddDays(1));
        Assert.False(waiver.IsActive);

        // Reativar de novo não é fato novo.
        waiver.Reactivate(now.AddDays(2));
        Assert.Equal(now.AddDays(1), waiver.ReactivatedAt);
    }

    [Fact]
    public void Dispatch_close_should_set_closed_status_once()
    {
        var dispatch = Dispatch.Create(
            projectId: 1,
            periodStart: new DateOnly(2026, 6, 1),
            periodEnd: new DateOnly(2026, 6, 30),
            format: NpsFormFormat.Simplified,
            language: NpsLanguage.Portuguese,
            createdBy: "ops",
            now: DateTimeOffset.UtcNow);

        dispatch.Close(DateTimeOffset.UtcNow);

        Assert.Equal(NpsDispatchStatus.Closed, dispatch.Status);
        Assert.NotNull(dispatch.ClosedAt);
    }
}
