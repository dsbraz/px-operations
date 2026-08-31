using PxOperations.Domain.Abstractions;
using PxOperations.Domain.Exceptions;

namespace PxOperations.Domain.Nps;

public sealed class SurveyResponse : AggregateRoot<int>
{
    private SurveyResponse() : base(default) { }

    public int ProjectId { get; private set; }
    public int DispatchId { get; private set; }
    public int TargetId { get; private set; }
    public int? ContactId { get; private set; }
    public NpsFormFormat Format { get; private set; }
    public int Score { get; private set; }
    public NpsClassification Classification { get; private set; }
    public int? Quality { get; private set; }
    public int? Schedule { get; private set; }
    public int? Communication { get; private set; }
    public int? BusinessValue { get; private set; }
    public string? Comment { get; private set; }
    public string? RespondentName { get; private set; }
    public string? RespondentEmail { get; private set; }
    public string? NormalizedRespondentEmail { get; private set; }
    public DateTimeOffset SubmittedAt { get; private set; }

    public static SurveyResponse Submit(
        SurveyResponseContext context,
        int score,
        int? quality,
        int? schedule,
        int? communication,
        int? businessValue,
        string? comment,
        string? respondentName,
        string? respondentEmail,
        DateTimeOffset now)
    {
        EnsureAvailable(context, respondentEmail, now);
        NpsScale.ValidateScore(score);

        var aspects = new[] { quality, schedule, communication, businessValue };
        if (context.Format == NpsFormFormat.Simplified && aspects.Any(value => value.HasValue))
        {
            throw new BusinessRuleValidationException("Simplified NPS responses cannot contain aspects.");
        }

        foreach (var aspect in aspects)
        {
            NpsScale.ValidateAspect(aspect);
        }

        var trimmedComment = TrimAndValidate(comment, 2000, "Comment");
        var trimmedName = TrimAndValidate(respondentName, 200, "Respondent name");
        var trimmedEmail = TrimAndValidate(respondentEmail, 320, "Respondent email");

        return new SurveyResponse
        {
            ProjectId = context.ProjectId,
            DispatchId = context.DispatchId,
            TargetId = context.TargetId,
            ContactId = context.ContactId,
            Format = context.Format,
            Score = score,
            Classification = NpsScale.Classify(score),
            Quality = quality,
            Schedule = schedule,
            Communication = communication,
            BusinessValue = businessValue,
            Comment = trimmedComment,
            RespondentName = trimmedName,
            RespondentEmail = trimmedEmail,
            NormalizedRespondentEmail = trimmedEmail?.ToLowerInvariant(),
            SubmittedAt = now
        };
    }

    private static void EnsureAvailable(SurveyResponseContext context, string? respondentEmail, DateTimeOffset now)
    {
        if (context.IsWaived)
        {
            throw new BusinessStateConflictException("NPS collection is waived.");
        }

        if (context.DispatchStatus != NpsDispatchStatus.Open)
        {
            throw new BusinessStateConflictException("NPS dispatch is closed.");
        }

        if (NpsCollectionPolicy.IsExpired(context.ExpiresAt, now))
        {
            throw new BusinessStateConflictException("NPS dispatch is expired.");
        }

        if (context.ContactId.HasValue && context.IsTargetUsed)
        {
            throw new BusinessStateConflictException("NPS contact target was already answered.");
        }

        if (!string.IsNullOrWhiteSpace(respondentEmail) && context.HasDuplicateEmail)
        {
            throw new BusinessStateConflictException("This email already answered this NPS link.");
        }
    }

    private static string? TrimAndValidate(string? value, int maximumLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maximumLength)
        {
            throw new BusinessRuleValidationException($"{field} must not exceed {maximumLength} characters.");
        }

        return trimmed;
    }
}
