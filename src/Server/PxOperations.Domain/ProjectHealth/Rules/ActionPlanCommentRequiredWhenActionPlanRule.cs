using PxOperations.Domain.Rules;

namespace PxOperations.Domain.ProjectHealth.Rules;

public sealed class ActionPlanCommentRequiredWhenActionPlanRule(
    bool actionPlanNeeded,
    string? actionPlanComment) : IBusinessRule
{
    public string Message => "Action plan comment is required when an action plan is needed.";

    public bool IsBroken()
    {
        return actionPlanNeeded && string.IsNullOrWhiteSpace(actionPlanComment);
    }
}
