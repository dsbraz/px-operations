using PxOperations.Domain.Rules;

namespace PxOperations.Domain.ProjectHealth.Rules;

public sealed class ReporterEmailMustNotBeEmptyRule(string email) : IBusinessRule
{
    public string Message => "Reporter email must not be empty.";

    public bool IsBroken()
    {
        return string.IsNullOrWhiteSpace(email);
    }
}
