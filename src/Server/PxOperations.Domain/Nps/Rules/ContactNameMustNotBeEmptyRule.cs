using PxOperations.Domain.Rules;

namespace PxOperations.Domain.Nps.Rules;

public sealed class ContactNameMustNotBeEmptyRule(string name) : IBusinessRule
{
    public string Message => "Contact name must not be empty.";

    public bool IsBroken() => string.IsNullOrWhiteSpace(name);
}
