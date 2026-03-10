using PxOperations.Domain.Rules;

namespace PxOperations.Domain.Projects.Rules;

public sealed class ProjectNameMustNotBeEmptyRule(string name) : IBusinessRule
{
    public string Message => "Project name must not be empty.";

    public bool IsBroken()
    {
        return string.IsNullOrWhiteSpace(name);
    }
}
