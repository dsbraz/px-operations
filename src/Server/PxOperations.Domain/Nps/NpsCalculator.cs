namespace PxOperations.Domain.Nps;

public static class NpsCalculator
{
    public static NpsMetrics Calculate(IEnumerable<int> scores)
    {
        var values = scores.ToArray();
        if (values.Length == 0)
        {
            return NpsMetrics.Empty;
        }

        var classifications = values.Select(NpsScale.Classify).ToArray();
        var promoters = classifications.Count(value => value == NpsClassification.Promoter);
        var passives = classifications.Count(value => value == NpsClassification.Passive);
        var detractors = classifications.Count(value => value == NpsClassification.Detractor);
        var officialScore = Math.Round(
            ((decimal)promoters / values.Length * 100m) - ((decimal)detractors / values.Length * 100m),
            1);

        return new NpsMetrics(
            officialScore,
            Math.Round((decimal)values.Average(), 1),
            Percentage(detractors, values.Length),
            Percentage(passives, values.Length),
            Percentage(promoters, values.Length));
    }

    private static decimal Percentage(int count, int total)
        => Math.Round((decimal)count / total * 100m, 1);
}
