using PxOperations.Domain.Exceptions;
using PxOperations.Domain.Nps;

namespace PxOperations.Application.Features.Nps;

public static class NpsCodes
{
    public static NpsFormFormat ParseFormat(string value) => value.Trim().ToLowerInvariant() switch
    {
        "complete" => NpsFormFormat.Complete,
        "simplified" => NpsFormFormat.Simplified,
        _ => throw new BusinessRuleValidationException("Invalid NPS format.")
    };

    public static NpsLanguage ParseLanguage(string value) => value.Trim().ToLowerInvariant() switch
    {
        "pt" => NpsLanguage.Portuguese,
        "en" => NpsLanguage.English,
        "es" => NpsLanguage.Spanish,
        _ => throw new BusinessRuleValidationException("Invalid NPS language.")
    };

    public static string Format(NpsFormFormat value) => value == NpsFormFormat.Complete ? "complete" : "simplified";
    public static string FormatLabel(NpsFormFormat value) => value == NpsFormFormat.Complete ? "Completo" : "Simplificado";

    public static string Language(NpsLanguage value) => value switch
    {
        NpsLanguage.Portuguese => "pt",
        NpsLanguage.English => "en",
        _ => "es"
    };

    public static string LanguageLabel(NpsLanguage value) => value switch
    {
        NpsLanguage.Portuguese => "Português",
        NpsLanguage.English => "Inglês",
        _ => "Espanhol"
    };

    public static string Classification(NpsClassification value) => value switch
    {
        NpsClassification.Detractor => "detractor",
        NpsClassification.Passive => "passive",
        _ => "promoter"
    };

    public static string ClassificationLabel(NpsClassification value) => value switch
    {
        NpsClassification.Detractor => "Detrator",
        NpsClassification.Passive => "Neutro",
        _ => "Promotor"
    };
}
