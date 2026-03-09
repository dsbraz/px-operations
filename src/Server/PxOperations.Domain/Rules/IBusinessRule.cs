namespace PxOperations.Domain.Rules;

public interface IBusinessRule
{
    string Message { get; }

    bool IsBroken();
}
