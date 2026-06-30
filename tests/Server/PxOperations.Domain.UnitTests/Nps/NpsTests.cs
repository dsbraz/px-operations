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
            scope: null,
            schedule: null,
            quality: null,
            communication: null,
            tags: null,
            comment: "Great",
            respondentName: "Jane",
            respondentEmail: "jane@example.com",
            now: DateTimeOffset.UtcNow);

        Assert.Null(response.Scope);
        Assert.Null(response.Schedule);
        Assert.Null(response.Quality);
        Assert.Null(response.Communication);
        Assert.Null(response.Tags);
        Assert.Equal(NpsClassification.Promoter, response.Classification);
    }

    [Fact]
    public void Response_should_reject_score_outside_nps_range()
    {
        Assert.Throws<BusinessRuleValidationException>(() => SurveyResponse.Submit(
            1, 2, 3, null, 11, null, null, null, null, null, null, null, null, DateTimeOffset.UtcNow));
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
