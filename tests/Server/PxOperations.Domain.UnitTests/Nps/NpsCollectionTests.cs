using PxOperations.Domain.Exceptions;
using PxOperations.Domain.Nps;

namespace PxOperations.Domain.UnitTests.Nps;

public sealed class NpsCollectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Creating_a_dispatch_should_add_a_generic_target_and_expire_in_twenty_days()
    {
        var collection = NpsCollection.Create(projectId: 7);

        var dispatch = collection.CreateDispatch(
            NpsFormFormat.Complete,
            NpsLanguage.Portuguese,
            contactIds: [],
            genericToken: Guid.NewGuid(),
            contactTokens: [],
            Now);

        Assert.Equal(Now.AddDays(20), dispatch.ExpiresAt);
        Assert.Single(dispatch.Targets);
        Assert.True(dispatch.Targets.Single().IsGeneric);
    }

    [Fact]
    public void Creating_the_same_format_should_close_only_the_previous_dispatch_of_that_format()
    {
        var collection = NpsCollection.Create(7);
        var complete = CreateDispatch(collection, NpsFormFormat.Complete, Now);
        var simplified = CreateDispatch(collection, NpsFormFormat.Simplified, Now.AddDays(1));

        var replacement = CreateDispatch(collection, NpsFormFormat.Complete, Now.AddDays(2));

        Assert.Equal(Now.AddDays(2), complete.ClosedAt);
        Assert.True(simplified.IsOpen);
        Assert.True(replacement.IsOpen);
    }

    [Fact]
    public void Waiving_should_close_every_open_dispatch_and_block_a_second_waiver_or_new_dispatch()
    {
        var collection = NpsCollection.Create(7);
        var complete = CreateDispatch(collection, NpsFormFormat.Complete, Now);
        var simplified = CreateDispatch(collection, NpsFormFormat.Simplified, Now);

        collection.Waive("Contrato sem pesquisa", Now.AddDays(1));

        Assert.False(complete.IsOpen);
        Assert.False(simplified.IsOpen);
        Assert.Throws<BusinessStateConflictException>(() => collection.Waive("Outro", Now.AddDays(2)));
        Assert.Throws<BusinessStateConflictException>(() => CreateDispatch(collection, NpsFormFormat.Complete, Now.AddDays(2)));
    }

    [Fact]
    public void Waiver_reason_should_be_required_and_limited_to_five_hundred_characters()
    {
        var collection = NpsCollection.Create(7);

        Assert.Throws<BusinessRuleValidationException>(() => collection.Waive(" ", Now));
        Assert.Throws<BusinessRuleValidationException>(() => collection.Waive(new string('a', 501), Now));
    }

    [Fact]
    public void Reactivating_should_remove_only_the_waiver_and_preserve_history()
    {
        var collection = NpsCollection.Create(7);
        CreateDispatch(collection, NpsFormFormat.Complete, Now);
        collection.Waive("Contrato sem pesquisa", Now.AddDays(1));

        collection.Reactivate();

        Assert.False(collection.IsWaived);
        Assert.Single(collection.Dispatches);
        Assert.Throws<ResourceNotFoundException>(() => collection.Reactivate());
    }

    [Theory]
    [InlineData(6, false)]
    [InlineData(5, true)]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void Expiration_warning_should_cover_only_the_last_five_valid_days(int daysRemaining, bool expected)
    {
        Assert.Equal(expected, NpsCollectionPolicy.IsExpiringSoon(Now.AddDays(daysRemaining), Now));
    }

    [Fact]
    public void A_dispatch_should_be_expired_at_exactly_twenty_days()
    {
        Assert.False(NpsCollectionPolicy.IsExpired(Now.AddDays(20), Now.AddDays(20).AddTicks(-1)));
        Assert.True(NpsCollectionPolicy.IsExpired(Now.AddDays(20), Now.AddDays(20)));
    }

    [Theory]
    [InlineData(45, NpsCollectionStage.Current)]
    [InlineData(46, NpsCollectionStage.Recollection)]
    public void Responded_open_collection_should_use_the_forty_five_day_boundary(
        int daysSinceResponse,
        NpsCollectionStage expected)
    {
        var openDispatches = new[] { new NpsOpenDispatchState(1, NpsFormFormat.Complete, Now.AddDays(10), true) };

        var stage = NpsCollectionPolicy.DetermineStage(false, openDispatches, Now.AddDays(-daysSinceResponse), Now);

        Assert.Equal(expected, stage);
    }

    [Fact]
    public void Stage_precedence_should_be_waived_then_waiting_then_no_link()
    {
        var unanswered = new[] { new NpsOpenDispatchState(1, NpsFormFormat.Complete, Now.AddDays(10), false) };

        Assert.Equal(NpsCollectionStage.Waived, NpsCollectionPolicy.DetermineStage(true, unanswered, null, Now));
        Assert.Equal(NpsCollectionStage.AwaitingResponse, NpsCollectionPolicy.DetermineStage(false, unanswered, null, Now));
        Assert.Equal(NpsCollectionStage.NoLink, NpsCollectionPolicy.DetermineStage(false, [], Now.AddDays(-100), Now));
    }

    [Theory]
    [InlineData(89, false)]
    [InlineData(90, true)]
    public void Overdue_should_use_the_ninety_day_boundary(int daysSinceResponse, bool expected)
    {
        Assert.Equal(expected, NpsCollectionPolicy.IsOverdue(false, false, Now.AddDays(-daysSinceResponse), Now));
        Assert.False(NpsCollectionPolicy.IsOverdue(true, false, Now.AddDays(-daysSinceResponse), Now));
        Assert.False(NpsCollectionPolicy.IsOverdue(false, true, Now.AddDays(-daysSinceResponse), Now));
    }

    [Fact]
    public void Primary_action_should_follow_stage_and_link_expiration_precedence()
    {
        var valid = new NpsOpenDispatchState(1, NpsFormFormat.Complete, Now.AddDays(3), false);
        var expired = new NpsOpenDispatchState(2, NpsFormFormat.Simplified, Now.AddDays(-2), false);

        Assert.Equal(NpsPrimaryActionKind.Reactivate, NpsCollectionPolicy.DeterminePrimaryAction(NpsCollectionStage.Waived, [], null, Now)!.Kind);
        Assert.Equal(NpsPrimaryActionKind.GenerateLink, NpsCollectionPolicy.DeterminePrimaryAction(NpsCollectionStage.NoLink, [], null, Now)!.Kind);
        Assert.Equal(NpsFormFormat.Complete, NpsCollectionPolicy.DeterminePrimaryAction(NpsCollectionStage.NoLink, [], null, Now)!.Format);
        Assert.Equal(2, NpsCollectionPolicy.DeterminePrimaryAction(NpsCollectionStage.AwaitingResponse, [valid, expired], null, Now)!.DispatchId);
        Assert.Equal(NpsPrimaryActionKind.GenerateLink, NpsCollectionPolicy.DeterminePrimaryAction(NpsCollectionStage.AwaitingResponse, [valid, expired], null, Now)!.Kind);
        Assert.Equal(1, NpsCollectionPolicy.DeterminePrimaryAction(NpsCollectionStage.AwaitingResponse, [valid], null, Now)!.DispatchId);
        Assert.Equal(NpsPrimaryActionKind.CopyLink, NpsCollectionPolicy.DeterminePrimaryAction(NpsCollectionStage.AwaitingResponse, [valid], null, Now)!.Kind);
        Assert.Null(NpsCollectionPolicy.DeterminePrimaryAction(NpsCollectionStage.Current, [valid with { HasResponses = true }], null, Now));
    }

    [Theory]
    [InlineData(true, true, NpsProjectResultStatus.Responded)]
    [InlineData(true, false, NpsProjectResultStatus.Responded)]
    [InlineData(false, true, NpsProjectResultStatus.LinkGenerated)]
    [InlineData(false, false, NpsProjectResultStatus.Pending)]
    public void Project_result_status_should_prioritize_responses_then_any_open_dispatch(
        bool hasResponses,
        bool hasOpenDispatch,
        NpsProjectResultStatus expected)
    {
        Assert.Equal(expected, NpsProjectResultPolicy.DetermineStatus(hasResponses, hasOpenDispatch));
    }

    [Fact]
    public void Expired_but_open_dispatch_should_still_be_link_generated()
    {
        var collection = NpsCollection.Create(7);
        var dispatch = CreateDispatch(collection, NpsFormFormat.Complete, Now.AddDays(-21));

        Assert.True(NpsCollectionPolicy.IsExpired(dispatch.ExpiresAt, Now));
        Assert.True(dispatch.IsOpen);
        Assert.Equal(NpsProjectResultStatus.LinkGenerated, NpsProjectResultPolicy.DetermineStatus(false, dispatch.IsOpen));
    }

    private static Dispatch CreateDispatch(NpsCollection collection, NpsFormFormat format, DateTimeOffset now)
        => collection.CreateDispatch(format, NpsLanguage.Portuguese, [], Guid.NewGuid(), [], now);
}
