using PxOperations.Domain.Exceptions;
using PxOperations.Domain.Nps;

namespace PxOperations.Domain.UnitTests.Nps;

public sealed class SurveyResponseTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Generic_target_should_accept_multiple_anonymous_responses()
    {
        var context = Context();

        var response = Submit(context);

        Assert.Null(response.NormalizedRespondentEmail);
        Assert.Equal(NpsClassification.Promoter, response.Classification);
    }

    [Fact]
    public void Respondent_email_should_be_normalized()
    {
        var response = Submit(Context(), respondentEmail: "  PERSON@Example.COM ");

        Assert.Equal("person@example.com", response.NormalizedRespondentEmail);
        Assert.Equal("PERSON@Example.COM", response.RespondentEmail);
    }

    [Fact]
    public void Duplicate_normalized_email_on_the_same_generic_target_should_be_rejected()
    {
        Assert.Throws<BusinessStateConflictException>(() => Submit(
            Context(hasDuplicateEmail: true),
            respondentEmail: "person@example.com"));
    }

    [Fact]
    public void Contact_target_should_remain_single_use()
    {
        Assert.Throws<BusinessStateConflictException>(() => Submit(Context(contactId: 3, isTargetUsed: true)));
    }

    [Fact]
    public void Simplified_form_should_reject_aspects_instead_of_ignoring_them()
    {
        Assert.Throws<BusinessRuleValidationException>(() => Submit(Context(format: NpsFormFormat.Simplified), quality: 5));
    }

    [Theory]
    [InlineData(NpsDispatchStatus.Closed, false, false)]
    [InlineData(NpsDispatchStatus.Open, true, false)]
    [InlineData(NpsDispatchStatus.Open, false, true)]
    public void Closed_expired_or_waived_dispatch_should_reject_a_response(
        NpsDispatchStatus status,
        bool expired,
        bool waived)
    {
        var expiresAt = expired ? Now : Now.AddDays(1);

        Assert.Throws<BusinessStateConflictException>(() => Submit(Context(status: status, expiresAt: expiresAt, isWaived: waived)));
    }

    [Fact]
    public void Response_should_reject_text_over_the_existing_limits()
    {
        Assert.Throws<BusinessRuleValidationException>(() => Submit(Context(), comment: new string('a', 2001)));
        Assert.Throws<BusinessRuleValidationException>(() => Submit(Context(), respondentName: new string('a', 201)));
        Assert.Throws<BusinessRuleValidationException>(() => Submit(Context(), respondentEmail: $"{new string('a', 309)}@example.com"));
    }

    private static SurveyResponse Submit(
        SurveyResponseContext context,
        int? quality = null,
        string? comment = null,
        string? respondentName = null,
        string? respondentEmail = null)
        => SurveyResponse.Submit(
            context,
            score: 10,
            quality,
            schedule: null,
            communication: null,
            businessValue: null,
            comment,
            respondentName,
            respondentEmail,
            Now);

    private static SurveyResponseContext Context(
        NpsFormFormat format = NpsFormFormat.Complete,
        NpsDispatchStatus status = NpsDispatchStatus.Open,
        DateTimeOffset? expiresAt = null,
        bool isWaived = false,
        int? contactId = null,
        bool isTargetUsed = false,
        bool hasDuplicateEmail = false)
        => new(
            ProjectId: 1,
            DispatchId: 2,
            TargetId: 3,
            ContactId: contactId,
            Format: format,
            DispatchStatus: status,
            ExpiresAt: expiresAt ?? Now.AddDays(1),
            IsWaived: isWaived,
            IsTargetUsed: isTargetUsed,
            HasDuplicateEmail: hasDuplicateEmail);
}
