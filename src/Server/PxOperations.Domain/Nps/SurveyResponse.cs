using PxOperations.Domain.Abstractions;
using PxOperations.Domain.Nps.Calculation;
using PxOperations.Domain.Nps.Rules;
using PxOperations.Domain.Projects;
using PxOperations.Domain.Rules;

namespace PxOperations.Domain.Nps;

public sealed class SurveyResponse : AggregateRoot<int>
{
    private SurveyResponse() : base(default) { }

    public int ProjectId { get; private set; }
    public int DispatchId { get; private set; }
    public int TargetId { get; private set; }
    public int? ContactId { get; private set; }
    public int Score { get; private set; }
    public NpsClassification Classification { get; private set; }
    public int? Scope { get; private set; }
    public int? Schedule { get; private set; }
    public int? Quality { get; private set; }
    public int? Communication { get; private set; }
    public string? Tags { get; private set; }
    public string? Comment { get; private set; }
    public string? RespondentName { get; private set; }
    public string? RespondentEmail { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }
    public Project Project { get; private set; } = default!;
    public Dispatch Dispatch { get; private set; } = default!;
    public DispatchTarget Target { get; private set; } = default!;
    public Contact? Contact { get; private set; }

    public static SurveyResponse Submit(
        int projectId,
        int dispatchId,
        int targetId,
        int? contactId,
        int score,
        int? scope,
        int? schedule,
        int? quality,
        int? communication,
        string? tags,
        string? comment,
        string? respondentName,
        string? respondentEmail,
        DateTimeOffset now)
    {
        RuleChecker.Check(new NpsScoreMustBeInRangeRule(score));

        return new SurveyResponse
        {
            ProjectId = projectId,
            DispatchId = dispatchId,
            TargetId = targetId,
            ContactId = contactId,
            Score = score,
            Classification = NpsCalculator.Classify(score),
            Scope = scope,
            Schedule = schedule,
            Quality = quality,
            Communication = communication,
            Tags = string.IsNullOrWhiteSpace(tags) ? null : tags.Trim(),
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            RespondentName = string.IsNullOrWhiteSpace(respondentName) ? null : respondentName.Trim(),
            RespondentEmail = string.IsNullOrWhiteSpace(respondentEmail) ? null : respondentEmail.Trim(),
            SubmittedAt = now
        };
    }
}
