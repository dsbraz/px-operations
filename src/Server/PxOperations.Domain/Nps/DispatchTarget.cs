using PxOperations.Domain.Abstractions;

namespace PxOperations.Domain.Nps;

public sealed class DispatchTarget : Entity<int>
{
    private DispatchTarget() : base(default) { }

    public int DispatchId { get; private set; }
    public int? ContactId { get; private set; }
    public Guid Token { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsGeneric => ContactId is null;

    internal static DispatchTarget CreateContact(int contactId, Guid token, DateTimeOffset now)
        => new() { ContactId = contactId, Token = token, CreatedAt = now };

    internal static DispatchTarget CreateGeneric(Guid token, DateTimeOffset now)
        => new() { Token = token, CreatedAt = now };
}
