using PxOperations.Domain.Abstractions;
using PxOperations.Domain.Nps.Rules;
using PxOperations.Domain.Projects;
using PxOperations.Domain.Rules;

namespace PxOperations.Domain.Nps;

/// <summary>
/// F6/D9: o NPS não tem "projeto encerrado". Existe um estado só — coleta
/// dispensada — marcado à mão e reversível a qualquer momento. É a única coisa
/// que tira card do quadro, e por isso a volta atrás faz parte do fluxo.
///
/// Modelado como registro próprio, e não como flag no projeto, para que a
/// dispensa e a reativação deixem rastro: quem dispensou, quando e por quê.
/// </summary>
public sealed class CollectionWaiver : AggregateRoot<int>
{
    private CollectionWaiver() : base(default) { }

    public int ProjectId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTimeOffset DismissedAt { get; private set; }
    public DateTimeOffset? ReactivatedAt { get; private set; }
    public Project Project { get; private set; } = default!;

    public bool IsActive => ReactivatedAt is null;

    public static CollectionWaiver Dismiss(int projectId, string reason, DateTimeOffset now)
    {
        RuleChecker.Check(new CollectionWaiverReasonMustNotBeEmptyRule(reason));

        return new CollectionWaiver
        {
            ProjectId = projectId,
            Reason = reason.Trim(),
            DismissedAt = now
        };
    }

    /// <summary>
    /// Reativar duas vezes não é erro nem fato novo: a segunda é ruído.
    /// </summary>
    public void Reactivate(DateTimeOffset now)
    {
        if (ReactivatedAt is not null)
        {
            return;
        }

        ReactivatedAt = now;
    }
}
