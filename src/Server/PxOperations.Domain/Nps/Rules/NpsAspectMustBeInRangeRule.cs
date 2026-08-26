using PxOperations.Domain.Rules;

namespace PxOperations.Domain.Nps.Rules;

/// <summary>
/// D10: aspectos da entrega vão de 1 a 5, escala mais curta que a da nota de
/// recomendação porque são quatro perguntas e a natureza é outra.
/// Nulo é válido: o formato Simplificado não coleta aspecto nenhum.
/// </summary>
public sealed class NpsAspectMustBeInRangeRule(int? value) : IBusinessRule
{
    public string Message => "NPS aspect must be between 1 and 5.";

    // Padrão relacional não casa com null, então o nulo passa sem cláusula extra.
    public bool IsBroken() => value is < 1 or > 5;
}
