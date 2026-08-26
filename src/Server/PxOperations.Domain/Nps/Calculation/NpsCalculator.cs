namespace PxOperations.Domain.Nps.Calculation;

public static class NpsCalculator
{
    // D10 moveu a régua com a escala: detrator é 1 a 6, neutro 7 a 8,
    // promotor 9 a 10. O <= 6 continua correto para o histórico em 0 a 10.
    public static NpsClassification Classify(int score) => score switch
    {
        <= 6 => NpsClassification.Detractor,
        <= 8 => NpsClassification.Passive,
        _ => NpsClassification.Promoter
    };

    public static decimal CalculateOfficialScore(IEnumerable<NpsClassification> classifications)
    {
        var values = classifications.ToList();
        if (values.Count == 0)
        {
            return 0;
        }

        var promoters = values.Count(c => c == NpsClassification.Promoter);
        var detractors = values.Count(c => c == NpsClassification.Detractor);

        return Math.Round(((decimal)promoters / values.Count * 100) - ((decimal)detractors / values.Count * 100), 1);
    }

    public static IReadOnlyDictionary<NpsClassification, int> Distribution(IEnumerable<NpsClassification> classifications)
    {
        var values = classifications.ToList();
        return new Dictionary<NpsClassification, int>
        {
            [NpsClassification.Detractor] = values.Count(c => c == NpsClassification.Detractor),
            [NpsClassification.Passive] = values.Count(c => c == NpsClassification.Passive),
            [NpsClassification.Promoter] = values.Count(c => c == NpsClassification.Promoter)
        };
    }
}
