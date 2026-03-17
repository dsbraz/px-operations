using PxOperations.Domain.Abstractions;
using PxOperations.Domain.Milestones.Rules;
using PxOperations.Domain.Projects;
using PxOperations.Domain.Rules;

namespace PxOperations.Domain.Milestones;

public sealed class Milestone : AggregateRoot<int>
{
    private Milestone() : base(default) { }

    public int ProjectId { get; private set; }
    public MilestoneType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DateOnly Date { get; private set; }
    public TimeOnly? Time { get; private set; }
    public string? Notes { get; private set; }
    public Project Project { get; private set; } = default!;

    public static Milestone Create(
        int projectId,
        MilestoneType type,
        string title,
        DateOnly date,
        TimeOnly? time,
        string? notes)
    {
        RuleChecker.Check(new MilestoneTitleMustNotBeEmptyRule(title));

        return new Milestone
        {
            ProjectId = projectId,
            Type = type,
            Title = title,
            Date = date,
            Time = time,
            Notes = notes
        };
    }

    public void Update(
        int projectId,
        MilestoneType type,
        string title,
        DateOnly date,
        TimeOnly? time,
        string? notes)
    {
        RuleChecker.Check(new MilestoneTitleMustNotBeEmptyRule(title));

        ProjectId = projectId;
        Type = type;
        Title = title;
        Date = date;
        Time = time;
        Notes = notes;
    }
}
