using PxOperations.Domain.Abstractions;
using PxOperations.Domain.Exceptions;

namespace PxOperations.Domain.Nps;

public sealed class NpsCollection : AggregateRoot<int>
{
    private readonly List<Dispatch> _dispatches = [];

    private NpsCollection() : base(default) { }

    public int ProjectId { get; private set; }
    public string? WaiverReason { get; private set; }
    public DateTimeOffset? WaivedAt { get; private set; }
    public bool IsWaived => WaivedAt.HasValue;
    public IReadOnlyCollection<Dispatch> Dispatches => _dispatches.AsReadOnly();

    public static NpsCollection Create(int projectId) => new() { ProjectId = projectId };

    public Dispatch CreateDispatch(
        NpsFormFormat format,
        NpsLanguage language,
        IReadOnlyCollection<int> contactIds,
        Guid genericToken,
        IReadOnlyCollection<Guid> contactTokens,
        DateTimeOffset now)
    {
        if (IsWaived)
        {
            throw new BusinessStateConflictException("Waived NPS collections cannot create dispatches.");
        }

        if (contactIds.Count != contactTokens.Count)
        {
            throw new ArgumentException("A token is required for every contact.", nameof(contactTokens));
        }

        foreach (var open in _dispatches.Where(item => item.IsOpen && item.Format == format))
        {
            open.Close(now);
        }

        var dispatch = Dispatch.Create(format, language, now);
        dispatch.AddGenericTarget(genericToken, now);

        foreach (var pair in contactIds.Zip(contactTokens))
        {
            dispatch.AddContactTarget(pair.First, pair.Second, now);
        }

        _dispatches.Add(dispatch);
        return dispatch;
    }

    public void Waive(string reason, DateTimeOffset now)
    {
        if (IsWaived)
        {
            throw new BusinessStateConflictException("NPS collection is already waived.");
        }

        var trimmedReason = reason.Trim();
        if (trimmedReason.Length is 0 or > 500)
        {
            throw new BusinessRuleValidationException("Waiver reason must contain between 1 and 500 characters.");
        }

        foreach (var dispatch in _dispatches.Where(item => item.IsOpen))
        {
            dispatch.Close(now);
        }

        WaiverReason = trimmedReason;
        WaivedAt = now;
    }

    public void Reactivate()
    {
        if (!IsWaived)
        {
            throw new ResourceNotFoundException("NPS collection waiver was not found.");
        }

        WaiverReason = null;
        WaivedAt = null;
    }
}
