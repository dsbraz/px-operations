using PxOperations.Domain.Abstractions;
using PxOperations.Domain.Exceptions;
using PxOperations.Domain.Rules;

namespace PxOperations.Domain.UnitTests.Abstractions;

public sealed class DomainBuildingBlocksTests
{
    [Fact]
    public void Value_object_equality_should_compare_components()
    {
        var first = new Money("BRL", 100m);
        var second = new Money("BRL", 100m);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Aggregate_root_should_dequeue_domain_events()
    {
        var aggregate = new TradingDesk(Guid.NewGuid());

        aggregate.MarkAsReady();

        var domainEvents = aggregate.DequeueDomainEvents();

        Assert.Single(domainEvents);
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void Specification_and_should_only_match_when_both_sides_match()
    {
        var specification = new PositiveAmountSpecification().And(new LargeAmountSpecification());

        Assert.True(specification.IsSatisfiedBy(100m));
        Assert.False(specification.IsSatisfiedBy(5m));
        Assert.False(specification.IsSatisfiedBy(-1m));
    }

    [Fact]
    public void Rule_checker_should_throw_for_broken_rules()
    {
        var rule = new AlwaysBrokenRule();

        var exception = Assert.Throws<BusinessRuleValidationException>(() => RuleChecker.Check(rule));

        Assert.Equal("Broken rule.", exception.Message);
    }

    private sealed class Money(string currency, decimal amount) : ValueObject
    {
        public string Currency { get; } = currency;

        public decimal Amount { get; } = amount;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Currency;
            yield return Amount;
        }
    }

    private sealed class TradingDesk(Guid id) : AggregateRoot<Guid>(id)
    {
        public void MarkAsReady()
        {
            RaiseDomainEvent(new TradingDeskReadyDomainEvent(Guid.NewGuid(), DateTimeOffset.UtcNow));
        }
    }

    private sealed record TradingDeskReadyDomainEvent(Guid Id, DateTimeOffset OccurredOnUtc)
        : DomainEvent(Id, OccurredOnUtc);

    private sealed class PositiveAmountSpecification : Specification<decimal>
    {
        public override bool IsSatisfiedBy(decimal candidate)
        {
            return candidate > 0;
        }
    }

    private sealed class LargeAmountSpecification : Specification<decimal>
    {
        public override bool IsSatisfiedBy(decimal candidate)
        {
            return candidate >= 100m;
        }
    }

    private sealed class AlwaysBrokenRule : IBusinessRule
    {
        public string Message => "Broken rule.";

        public bool IsBroken()
        {
            return true;
        }
    }
}
