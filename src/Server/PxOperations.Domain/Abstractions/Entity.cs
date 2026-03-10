namespace PxOperations.Domain.Abstractions;

public abstract class Entity<TId> where TId : notnull
{
    protected Entity(TId id)
    {
        Id = id;
    }

    public TId Id { get; private set; }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> other
            && GetType() == other.GetType()
            && EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }
}
