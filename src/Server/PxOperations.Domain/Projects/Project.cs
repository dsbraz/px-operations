using PxOperations.Domain.Abstractions;
using PxOperations.Domain.Projects.Rules;
using PxOperations.Domain.Rules;

namespace PxOperations.Domain.Projects;

public sealed class Project : AggregateRoot<int>
{
    private Project() : base(default) { }

    public DeliveryCenter Dc { get; private set; }
    public ProjectStatus Status { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Client { get; private set; }
    public ProjectType Type { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string? DeliveryManager { get; private set; }
    public RenewalStatus Renewal { get; private set; }
    public string? RenewalObservation { get; private set; }

    public static Project Create(
        DeliveryCenter dc,
        ProjectStatus status,
        string name,
        string? client,
        ProjectType type,
        DateOnly? startDate,
        DateOnly? endDate,
        string? deliveryManager,
        RenewalStatus renewal,
        string? renewalObservation)
    {
        RuleChecker.Check(new ProjectNameMustNotBeEmptyRule(name));
        RuleChecker.Check(new ProjectEndDateMustNotPrecedeStartDateRule(startDate, endDate));

        var project = new Project
        {
            Dc = dc,
            Status = status,
            Name = name,
            Client = client,
            Type = type,
            StartDate = startDate,
            EndDate = endDate,
            DeliveryManager = deliveryManager,
            Renewal = renewal,
            RenewalObservation = renewalObservation
        };

        return project;
    }

    public void Update(
        DeliveryCenter dc,
        ProjectStatus status,
        string name,
        string? client,
        ProjectType type,
        DateOnly? startDate,
        DateOnly? endDate,
        string? deliveryManager,
        RenewalStatus renewal,
        string? renewalObservation)
    {
        RuleChecker.Check(new ProjectNameMustNotBeEmptyRule(name));
        RuleChecker.Check(new ProjectEndDateMustNotPrecedeStartDateRule(startDate, endDate));

        Dc = dc;
        Status = status;
        Name = name;
        Client = client;
        Type = type;
        StartDate = startDate;
        EndDate = endDate;
        DeliveryManager = deliveryManager;
        Renewal = renewal;
        RenewalObservation = renewalObservation;
    }
}
