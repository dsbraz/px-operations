using PxOperations.Domain.Rules;

namespace PxOperations.Domain.Nps.Rules;

public sealed class DispatchPeriodMustBeValidRule(DateOnly periodStart, DateOnly periodEnd) : IBusinessRule
{
    public string Message => "Dispatch period end date must not precede start date.";

    public bool IsBroken() => periodEnd < periodStart;
}
