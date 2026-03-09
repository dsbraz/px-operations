using PxOperations.Domain.Exceptions;

namespace PxOperations.Domain.Rules;

public static class RuleChecker
{
    public static void Check(IBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw new BusinessRuleValidationException(rule.Message);
        }
    }
}
