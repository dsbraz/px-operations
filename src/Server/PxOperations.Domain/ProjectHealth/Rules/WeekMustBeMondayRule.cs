using PxOperations.Domain.Rules;

namespace PxOperations.Domain.ProjectHealth.Rules;

public sealed class WeekMustBeMondayRule(DateOnly week) : IBusinessRule
{
    public string Message => "Week must be a Monday.";

    public bool IsBroken()
    {
        return week.DayOfWeek != DayOfWeek.Monday;
    }
}
