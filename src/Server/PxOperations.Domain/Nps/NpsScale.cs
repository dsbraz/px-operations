using PxOperations.Domain.Exceptions;

namespace PxOperations.Domain.Nps;

public static class NpsScale
{
    public const int MinimumScore = 1;
    public const int MaximumScore = 10;
    public const int MinimumAspect = 1;
    public const int MaximumAspect = 5;

    public static NpsClassification Classify(int score)
    {
        ValidateScore(score);

        return score switch
        {
            <= 6 => NpsClassification.Detractor,
            <= 8 => NpsClassification.Passive,
            _ => NpsClassification.Promoter
        };
    }

    public static void ValidateScore(int score)
    {
        if (score is < MinimumScore or > MaximumScore)
        {
            throw new BusinessRuleValidationException("NPS score must be between 1 and 10.");
        }
    }

    public static void ValidateAspect(int? value)
    {
        if (value is < MinimumAspect or > MaximumAspect)
        {
            throw new BusinessRuleValidationException("NPS aspect score must be between 1 and 5.");
        }
    }
}
