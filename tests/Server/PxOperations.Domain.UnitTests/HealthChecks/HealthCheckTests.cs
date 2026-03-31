using PxOperations.Domain.Exceptions;
using PxOperations.Domain.HealthChecks;

namespace PxOperations.Domain.UnitTests.HealthChecks;

public sealed class HealthCheckTests
{
    private static readonly DateOnly Monday = new(2026, 3, 30);

    [Fact]
    public void Create_should_set_all_properties()
    {
        var hc = HealthCheck.Create(
            projectId: 1,
            subProject: "Squad Pagamentos",
            week: Monday,
            reporterEmail: "joao@brq.com",
            practicesCount: 4,
            scope: RagStatus.Green,
            schedule: RagStatus.Yellow,
            quality: RagStatus.Green,
            satisfaction: RagStatus.Green,
            expansionOpportunity: true,
            expansionComment: "Novas trilhas de UX",
            actionPlanNeeded: false,
            highlights: "Sprint entregue no prazo.");

        Assert.Equal(1, hc.ProjectId);
        Assert.Equal("Squad Pagamentos", hc.SubProject);
        Assert.Equal(Monday, hc.Week);
        Assert.Equal("joao@brq.com", hc.ReporterEmail);
        Assert.Equal(4, hc.PracticesCount);
        Assert.Equal(RagStatus.Green, hc.Scope);
        Assert.Equal(RagStatus.Yellow, hc.Schedule);
        Assert.Equal(RagStatus.Green, hc.Quality);
        Assert.Equal(RagStatus.Green, hc.Satisfaction);
        Assert.True(hc.ExpansionOpportunity);
        Assert.Equal("Novas trilhas de UX", hc.ExpansionComment);
        Assert.False(hc.ActionPlanNeeded);
        Assert.Equal("Sprint entregue no prazo.", hc.Highlights);
    }

    [Fact]
    public void Create_should_compute_score_all_green_five_practices()
    {
        var hc = CreateValid(practicesCount: 5,
            scope: RagStatus.Green, schedule: RagStatus.Green,
            quality: RagStatus.Green, satisfaction: RagStatus.Green);

        Assert.Equal(10, hc.Score);
    }

    [Fact]
    public void Create_should_compute_score_all_red_zero_practices()
    {
        var hc = CreateValid(practicesCount: 0,
            scope: RagStatus.Red, schedule: RagStatus.Red,
            quality: RagStatus.Red, satisfaction: RagStatus.Red);

        Assert.Equal(0, hc.Score);
    }

    [Theory]
    [InlineData(2, RagStatus.Yellow, RagStatus.Yellow, RagStatus.Red, RagStatus.Green, 5)]
    [InlineData(1, RagStatus.Red, RagStatus.Red, RagStatus.Red, RagStatus.Red, 1)]
    [InlineData(3, RagStatus.Green, RagStatus.Yellow, RagStatus.Green, RagStatus.Green, 9)]
    [InlineData(0, RagStatus.Green, RagStatus.Green, RagStatus.Green, RagStatus.Green, 8)]
    public void Create_should_compute_score_with_mixed_values(
        int practicesCount, RagStatus scope, RagStatus schedule,
        RagStatus quality, RagStatus satisfaction, int expectedScore)
    {
        var hc = CreateValid(practicesCount, scope, schedule, quality, satisfaction);

        Assert.Equal(expectedScore, hc.Score);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_should_fail_when_highlights_is_empty(string? highlights)
    {
        var ex = Assert.Throws<BusinessRuleValidationException>(() =>
            CreateValid(highlights: highlights!));

        Assert.Equal("Highlights must not be empty.", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_should_fail_when_reporter_email_is_empty(string? email)
    {
        var ex = Assert.Throws<BusinessRuleValidationException>(() =>
            CreateValid(reporterEmail: email!));

        Assert.Equal("Reporter email must not be empty.", ex.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void Create_should_fail_when_practices_count_out_of_range(int count)
    {
        var ex = Assert.Throws<BusinessRuleValidationException>(() =>
            CreateValid(practicesCount: count));

        Assert.Equal("Practices count must be between 0 and 5.", ex.Message);
    }

    [Fact]
    public void Create_should_fail_when_week_is_not_monday()
    {
        var tuesday = new DateOnly(2026, 3, 31);

        var ex = Assert.Throws<BusinessRuleValidationException>(() =>
            CreateValid(week: tuesday));

        Assert.Equal("Week must be a Monday.", ex.Message);
    }

    [Fact]
    public void Create_should_fail_when_expansion_true_but_comment_empty()
    {
        var ex = Assert.Throws<BusinessRuleValidationException>(() =>
            HealthCheck.Create(
                projectId: 1,
                subProject: null,
                week: Monday,
                reporterEmail: "joao@brq.com",
                practicesCount: 3,
                scope: RagStatus.Green,
                schedule: RagStatus.Green,
                quality: RagStatus.Green,
                satisfaction: RagStatus.Green,
                expansionOpportunity: true,
                expansionComment: null,
                actionPlanNeeded: false,
                highlights: "Tudo certo."));

        Assert.Equal("Expansion comment is required when there is an expansion opportunity.", ex.Message);
    }

    [Fact]
    public void Create_should_accept_null_expansion_comment_when_no_expansion()
    {
        var hc = HealthCheck.Create(
            projectId: 1,
            subProject: null,
            week: Monday,
            reporterEmail: "joao@brq.com",
            practicesCount: 3,
            scope: RagStatus.Green,
            schedule: RagStatus.Green,
            quality: RagStatus.Green,
            satisfaction: RagStatus.Green,
            expansionOpportunity: false,
            expansionComment: null,
            actionPlanNeeded: false,
            highlights: "Tudo certo.");

        Assert.False(hc.ExpansionOpportunity);
        Assert.Null(hc.ExpansionComment);
    }

    [Fact]
    public void Update_should_recompute_score()
    {
        var hc = CreateValid(practicesCount: 5,
            scope: RagStatus.Green, schedule: RagStatus.Green,
            quality: RagStatus.Green, satisfaction: RagStatus.Green);

        Assert.Equal(10, hc.Score);

        hc.Update(
            projectId: 1,
            subProject: null,
            week: Monday,
            reporterEmail: "joao@brq.com",
            practicesCount: 0,
            scope: RagStatus.Red,
            schedule: RagStatus.Red,
            quality: RagStatus.Red,
            satisfaction: RagStatus.Red,
            expansionOpportunity: false,
            expansionComment: null,
            actionPlanNeeded: true,
            highlights: "Semana critica.");

        Assert.Equal(0, hc.Score);
    }

    [Fact]
    public void Update_should_validate_business_rules()
    {
        var hc = CreateValid();

        Assert.Throws<BusinessRuleValidationException>(() =>
            hc.Update(
                projectId: 1,
                subProject: null,
                week: Monday,
                reporterEmail: "joao@brq.com",
                practicesCount: 3,
                scope: RagStatus.Green,
                schedule: RagStatus.Green,
                quality: RagStatus.Green,
                satisfaction: RagStatus.Green,
                expansionOpportunity: false,
                expansionComment: null,
                actionPlanNeeded: false,
                highlights: ""));
    }

    private static HealthCheck CreateValid(
        int practicesCount = 3,
        RagStatus scope = RagStatus.Green,
        RagStatus schedule = RagStatus.Green,
        RagStatus quality = RagStatus.Green,
        RagStatus satisfaction = RagStatus.Green,
        DateOnly? week = null,
        string reporterEmail = "joao@brq.com",
        string highlights = "Tudo certo.")
    {
        return HealthCheck.Create(
            projectId: 1,
            subProject: null,
            week: week ?? Monday,
            reporterEmail: reporterEmail,
            practicesCount: practicesCount,
            scope: scope,
            schedule: schedule,
            quality: quality,
            satisfaction: satisfaction,
            expansionOpportunity: false,
            expansionComment: null,
            actionPlanNeeded: false,
            highlights: highlights);
    }
}
