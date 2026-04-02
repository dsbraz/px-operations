using PxOperations.Domain.Rules;

namespace PxOperations.Domain.ProjectHealth.Rules;

public sealed class HighlightsMustNotBeEmptyRule(string highlights) : IBusinessRule
{
    public string Message => "Highlights must not be empty.";

    public bool IsBroken()
    {
        return string.IsNullOrWhiteSpace(highlights);
    }
}
