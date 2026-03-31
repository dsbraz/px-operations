using PxOperations.Domain.Rules;

namespace PxOperations.Domain.HealthChecks.Rules;

public sealed class ExpansionCommentRequiredWhenExpansionRule(
    bool expansionOpportunity,
    string? expansionComment) : IBusinessRule
{
    public string Message => "Expansion comment is required when there is an expansion opportunity.";

    public bool IsBroken()
    {
        return expansionOpportunity && string.IsNullOrWhiteSpace(expansionComment);
    }
}
