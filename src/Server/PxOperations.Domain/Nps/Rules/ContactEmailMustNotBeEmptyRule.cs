using PxOperations.Domain.Rules;

namespace PxOperations.Domain.Nps.Rules;

public sealed class ContactEmailMustNotBeEmptyRule(string email) : IBusinessRule
{
    public string Message => "Contact email must not be empty.";

    public bool IsBroken() => string.IsNullOrWhiteSpace(email);
}
