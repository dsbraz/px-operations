using PxOperations.Domain.Rules;

namespace PxOperations.Domain.HealthChecks.Rules;

public sealed class PracticesCountMustBeInRangeRule(int count) : IBusinessRule
{
    public string Message => "Practices count must be between 0 and 5.";

    public bool IsBroken()
    {
        return count < 0 || count > 5;
    }
}
