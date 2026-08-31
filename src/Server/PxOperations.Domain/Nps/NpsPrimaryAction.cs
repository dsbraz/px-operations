namespace PxOperations.Domain.Nps;

public sealed record NpsPrimaryAction(
    NpsPrimaryActionKind Kind,
    NpsFormFormat? Format,
    int? DispatchId);
