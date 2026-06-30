using PxOperations.Domain.Rules;

namespace PxOperations.Domain.Nps.Rules;

public sealed class NpsScoreMustBeInRangeRule(int score) : IBusinessRule
{
    public string Message => "NPS score must be between 0 and 10.";

    public bool IsBroken() => score is < 0 or > 10;
}
