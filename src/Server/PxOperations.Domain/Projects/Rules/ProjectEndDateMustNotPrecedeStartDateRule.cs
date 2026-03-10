using PxOperations.Domain.Rules;

namespace PxOperations.Domain.Projects.Rules;

public sealed class ProjectEndDateMustNotPrecedeStartDateRule(
    DateOnly? startDate,
    DateOnly? endDate) : IBusinessRule
{
    public string Message => "Project end date must not precede start date.";

    public bool IsBroken()
    {
        return startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value;
    }
}
