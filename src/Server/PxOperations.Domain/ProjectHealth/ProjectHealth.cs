using PxOperations.Domain.Abstractions;
using PxOperations.Domain.ProjectHealth.Rules;
using PxOperations.Domain.Projects;
using PxOperations.Domain.Rules;

namespace PxOperations.Domain.ProjectHealth;

public sealed class ProjectHealth : AggregateRoot<int>
{
    private ProjectHealth() : base(default) { }

    public int ProjectId { get; private set; }
    public string? SubProject { get; private set; }
    public DateOnly Week { get; private set; }
    public string ReporterEmail { get; private set; } = string.Empty;
    public int PracticesCount { get; private set; }
    public RagStatus Scope { get; private set; }
    public RagStatus Schedule { get; private set; }
    public RagStatus Quality { get; private set; }
    public RagStatus Satisfaction { get; private set; }
    public bool ExpansionOpportunity { get; private set; }
    public string? ExpansionComment { get; private set; }
    public bool ActionPlanNeeded { get; private set; }
    public string Highlights { get; private set; } = string.Empty;
    public int Score { get; private set; }
    public Project Project { get; private set; } = default!;

    public static ProjectHealth Create(
        int projectId,
        string? subProject,
        DateOnly week,
        string reporterEmail,
        int practicesCount,
        RagStatus scope,
        RagStatus schedule,
        RagStatus quality,
        RagStatus satisfaction,
        bool expansionOpportunity,
        string? expansionComment,
        bool actionPlanNeeded,
        string highlights)
    {
        CheckRules(reporterEmail, practicesCount, week, expansionOpportunity, expansionComment, highlights);

        return new ProjectHealth
        {
            ProjectId = projectId,
            SubProject = subProject,
            Week = week,
            ReporterEmail = reporterEmail,
            PracticesCount = practicesCount,
            Scope = scope,
            Schedule = schedule,
            Quality = quality,
            Satisfaction = satisfaction,
            ExpansionOpportunity = expansionOpportunity,
            ExpansionComment = expansionComment,
            ActionPlanNeeded = actionPlanNeeded,
            Highlights = highlights,
            Score = ComputeScore(practicesCount, scope, schedule, quality, satisfaction)
        };
    }

    public void Update(
        int projectId,
        string? subProject,
        DateOnly week,
        string reporterEmail,
        int practicesCount,
        RagStatus scope,
        RagStatus schedule,
        RagStatus quality,
        RagStatus satisfaction,
        bool expansionOpportunity,
        string? expansionComment,
        bool actionPlanNeeded,
        string highlights)
    {
        CheckRules(reporterEmail, practicesCount, week, expansionOpportunity, expansionComment, highlights);

        ProjectId = projectId;
        SubProject = subProject;
        Week = week;
        ReporterEmail = reporterEmail;
        PracticesCount = practicesCount;
        Scope = scope;
        Schedule = schedule;
        Quality = quality;
        Satisfaction = satisfaction;
        ExpansionOpportunity = expansionOpportunity;
        ExpansionComment = expansionComment;
        ActionPlanNeeded = actionPlanNeeded;
        Highlights = highlights;
        Score = ComputeScore(practicesCount, scope, schedule, quality, satisfaction);
    }

    private static void CheckRules(
        string reporterEmail,
        int practicesCount,
        DateOnly week,
        bool expansionOpportunity,
        string? expansionComment,
        string highlights)
    {
        RuleChecker.Check(new ReporterEmailMustNotBeEmptyRule(reporterEmail));
        RuleChecker.Check(new PracticesCountMustBeInRangeRule(practicesCount));
        RuleChecker.Check(new WeekMustBeMondayRule(week));
        RuleChecker.Check(new ExpansionCommentRequiredWhenExpansionRule(expansionOpportunity, expansionComment));
        RuleChecker.Check(new HighlightsMustNotBeEmptyRule(highlights));
    }

    private static int ComputeScore(
        int practicesCount,
        RagStatus scope,
        RagStatus schedule,
        RagStatus quality,
        RagStatus satisfaction)
    {
        var practicesScore = practicesCount >= 3 ? 2 : practicesCount >= 1 ? 1 : 0;
        return practicesScore + RagScore(scope) + RagScore(schedule) + RagScore(quality) + RagScore(satisfaction);
    }

    private static int RagScore(RagStatus status) => status switch
    {
        RagStatus.Green => 2,
        RagStatus.Yellow => 1,
        RagStatus.Red => 0,
        _ => 0
    };
}
