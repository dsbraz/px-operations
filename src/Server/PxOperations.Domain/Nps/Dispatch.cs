using PxOperations.Domain.Abstractions;
using PxOperations.Domain.Nps.Rules;
using PxOperations.Domain.Projects;
using PxOperations.Domain.Rules;

namespace PxOperations.Domain.Nps;

public sealed class Dispatch : AggregateRoot<int>
{
    private readonly List<DispatchTarget> _targets = [];

    private Dispatch() : base(default) { }

    public int ProjectId { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public NpsFormFormat Format { get; private set; }
    public NpsLanguage Language { get; private set; }
    public NpsDispatchStatus Status { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>
    /// D7: todo link nasce com prazo. Fixo em 20 dias por decisão de produto —
    /// se um dia virar parâmetro por projeto, é esta constante que vira coluna.
    /// </summary>
    public static readonly TimeSpan LinkValidity = TimeSpan.FromDays(20);

    public DateTimeOffset ExpiresAt { get; private set; }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
    public Project Project { get; private set; } = default!;
    public IReadOnlyCollection<DispatchTarget> Targets => _targets.AsReadOnly();

    public static Dispatch Create(
        int projectId,
        DateOnly periodStart,
        DateOnly periodEnd,
        NpsFormFormat format,
        NpsLanguage language,
        string createdBy,
        DateTimeOffset now)
    {
        RuleChecker.Check(new DispatchPeriodMustBeValidRule(periodStart, periodEnd));

        return new Dispatch
        {
            ProjectId = projectId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            Format = format,
            Language = language,
            Status = NpsDispatchStatus.Open,
            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "system" : createdBy.Trim(),
            CreatedAt = now,
            ExpiresAt = now + LinkValidity
        };
    }

    public void Close(DateTimeOffset now)
    {
        if (Status == NpsDispatchStatus.Closed)
        {
            return;
        }

        Status = NpsDispatchStatus.Closed;
        ClosedAt = now;
    }
}
