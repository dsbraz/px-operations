namespace PxOperations.Domain.Abstractions;

public abstract record DomainEvent(Guid Id, DateTimeOffset OccurredOnUtc);
