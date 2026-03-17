using PxOperations.Domain.Rules;

namespace PxOperations.Domain.Milestones.Rules;

public sealed class MilestoneTitleMustNotBeEmptyRule(string title) : IBusinessRule
{
    public bool IsBroken() => string.IsNullOrWhiteSpace(title);

    public string Message => "Milestone title must not be empty.";
}
