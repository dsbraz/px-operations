using PxOperations.Domain.Abstractions;

namespace PxOperations.Domain.Nps;

public sealed class Dispatch : Entity<int>
{
    private readonly List<DispatchTarget> _targets = [];

    private Dispatch() : base(default) { }

    public int CollectionId { get; private set; }
    public NpsFormFormat Format { get; private set; }
    public NpsLanguage Language { get; private set; }
    public NpsDispatchStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public IReadOnlyCollection<DispatchTarget> Targets => _targets.AsReadOnly();
    public bool IsOpen => Status == NpsDispatchStatus.Open;

    internal static Dispatch Create(NpsFormFormat format, NpsLanguage language, DateTimeOffset now)
        => new()
        {
            Format = format,
            Language = language,
            Status = NpsDispatchStatus.Open,
            CreatedAt = now,
            ExpiresAt = now.AddDays(NpsCollectionPolicy.LinkValidityDays)
        };

    internal void AddGenericTarget(Guid token, DateTimeOffset now)
        => _targets.Add(DispatchTarget.CreateGeneric(token, now));

    internal void AddContactTarget(int contactId, Guid token, DateTimeOffset now)
        => _targets.Add(DispatchTarget.CreateContact(contactId, token, now));

    public void Close(DateTimeOffset now)
    {
        if (!IsOpen)
        {
            return;
        }

        Status = NpsDispatchStatus.Closed;
        ClosedAt = now;
    }
}
