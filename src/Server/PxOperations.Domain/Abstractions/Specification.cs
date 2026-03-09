namespace PxOperations.Domain.Abstractions;

public abstract class Specification<T>
{
    public abstract bool IsSatisfiedBy(T candidate);

    public Specification<T> And(Specification<T> other)
    {
        return new AndSpecification<T>(this, other);
    }

    private sealed class AndSpecification<TCandidate>(
        Specification<TCandidate> left,
        Specification<TCandidate> right) : Specification<TCandidate>
    {
        public override bool IsSatisfiedBy(TCandidate candidate)
        {
            return left.IsSatisfiedBy(candidate) && right.IsSatisfiedBy(candidate);
        }
    }
}
