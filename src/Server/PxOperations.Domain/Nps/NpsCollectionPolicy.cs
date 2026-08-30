namespace PxOperations.Domain.Nps;

public static class NpsCollectionPolicy
{
    public const int LinkValidityDays = 20;
    public const int ExpirationWarningDays = 5;
    public const int RecollectionDays = 45;
    public const int OverdueDays = 90;

    public static bool IsExpired(DateTimeOffset expiresAt, DateTimeOffset now) => now >= expiresAt;

    public static bool IsExpiringSoon(DateTimeOffset expiresAt, DateTimeOffset now)
        => !IsExpired(expiresAt, now) && expiresAt <= now.AddDays(ExpirationWarningDays);

    public static NpsCollectionStage DetermineStage(
        bool isWaived,
        IReadOnlyCollection<NpsOpenDispatchState> openDispatches,
        DateTimeOffset? lastResponseAt,
        DateTimeOffset now)
    {
        if (isWaived)
        {
            return NpsCollectionStage.Waived;
        }

        if (openDispatches.Any(dispatch => !dispatch.HasResponses))
        {
            return NpsCollectionStage.AwaitingResponse;
        }

        if (openDispatches.Count == 0)
        {
            return NpsCollectionStage.NoLink;
        }

        return lastResponseAt < now.AddDays(-RecollectionDays)
            ? NpsCollectionStage.Recollection
            : NpsCollectionStage.Current;
    }

    public static bool IsOverdue(
        bool isWaived,
        bool hasOpenDispatch,
        DateTimeOffset? lastResponseAt,
        DateTimeOffset now)
        => !isWaived &&
           !hasOpenDispatch &&
           (!lastResponseAt.HasValue || lastResponseAt.Value <= now.AddDays(-OverdueDays));

    public static NpsPrimaryAction? DeterminePrimaryAction(
        NpsCollectionStage stage,
        IReadOnlyCollection<NpsOpenDispatchState> openDispatches,
        NpsFormFormat? mostRecentFormat,
        DateTimeOffset now)
    {
        if (stage == NpsCollectionStage.Waived)
        {
            return new NpsPrimaryAction(NpsPrimaryActionKind.Reactivate, null, null);
        }

        if (stage == NpsCollectionStage.NoLink)
        {
            return new NpsPrimaryAction(NpsPrimaryActionKind.GenerateLink, NpsFormFormat.Complete, null);
        }

        if (stage == NpsCollectionStage.Recollection)
        {
            return new NpsPrimaryAction(
                NpsPrimaryActionKind.GenerateLink,
                mostRecentFormat ?? NpsFormFormat.Complete,
                null);
        }

        if (stage != NpsCollectionStage.AwaitingResponse)
        {
            return null;
        }

        var expired = openDispatches
            .Where(dispatch => IsExpired(dispatch.ExpiresAt, now))
            .OrderBy(dispatch => dispatch.ExpiresAt)
            .FirstOrDefault();
        if (expired is not null)
        {
            return new NpsPrimaryAction(NpsPrimaryActionKind.GenerateLink, expired.Format, expired.DispatchId);
        }

        var closest = openDispatches.OrderBy(dispatch => dispatch.ExpiresAt).First();
        return new NpsPrimaryAction(NpsPrimaryActionKind.CopyLink, closest.Format, closest.DispatchId);
    }
}
