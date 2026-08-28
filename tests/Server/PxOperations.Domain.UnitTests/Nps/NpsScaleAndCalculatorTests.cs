using PxOperations.Domain.Exceptions;
using PxOperations.Domain.Nps;
using PxOperations.Domain.Nps.Calculation;

namespace PxOperations.Domain.UnitTests.Nps;

public sealed class NpsScaleAndCalculatorTests
{
    [Theory]
    [InlineData(1, NpsClassification.Detractor)]
    [InlineData(6, NpsClassification.Detractor)]
    [InlineData(7, NpsClassification.Passive)]
    [InlineData(8, NpsClassification.Passive)]
    [InlineData(9, NpsClassification.Promoter)]
    [InlineData(10, NpsClassification.Promoter)]
    public void Classify_should_follow_the_one_to_ten_scale(int score, NpsClassification expected)
    {
        Assert.Equal(expected, NpsScale.Classify(score));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Classify_should_reject_scores_outside_the_scale(int score)
    {
        Assert.Throws<BusinessRuleValidationException>(() => NpsScale.Classify(score));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Aspect_should_accept_the_scale_boundaries(int value)
    {
        NpsScale.ValidateAspect(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Aspect_should_reject_values_outside_the_scale(int value)
    {
        Assert.Throws<BusinessRuleValidationException>(() => NpsScale.ValidateAspect(value));
    }

    [Fact]
    public void Calculate_should_return_empty_metrics_without_responses()
    {
        var result = NpsCalculator.Calculate([]);

        Assert.Null(result.OfficialScore);
        Assert.Null(result.AverageScore);
        Assert.Equal(0m, result.DetractorPercentage);
        Assert.Equal(0m, result.PassivePercentage);
        Assert.Equal(0m, result.PromoterPercentage);
    }

    [Fact]
    public void Calculate_should_round_the_official_score_only_after_subtracting_raw_percentages()
    {
        var result = NpsCalculator.Calculate([1, 7, 9, 9, 9, 9]);

        Assert.Equal(50.0m, result.OfficialScore);
        Assert.Equal(7.3m, result.AverageScore);
        Assert.Equal(16.7m, result.DetractorPercentage);
        Assert.Equal(16.7m, result.PassivePercentage);
        Assert.Equal(66.7m, result.PromoterPercentage);
    }
}
