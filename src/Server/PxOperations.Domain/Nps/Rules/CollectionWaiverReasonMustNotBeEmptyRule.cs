using PxOperations.Domain.Rules;

namespace PxOperations.Domain.Nps.Rules;

/// <summary>
/// F6 exige motivo na dispensa: sem ele o card some do quadro sem que ninguém
/// saiba por quê, e a reativação vira adivinhação.
/// </summary>
public sealed class CollectionWaiverReasonMustNotBeEmptyRule(string? reason) : IBusinessRule
{
    public string Message => "Collection waiver requires a reason.";

    public bool IsBroken() => string.IsNullOrWhiteSpace(reason);
}
