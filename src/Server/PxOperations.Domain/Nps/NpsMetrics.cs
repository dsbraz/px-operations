namespace PxOperations.Domain.Nps;

public sealed record NpsMetrics(
    decimal? OfficialScore,
    decimal? AverageScore,
    decimal DetractorPercentage,
    decimal PassivePercentage,
    decimal PromoterPercentage)
{
    public static NpsMetrics Empty { get; } = new(null, null, 0m, 0m, 0m);
}
