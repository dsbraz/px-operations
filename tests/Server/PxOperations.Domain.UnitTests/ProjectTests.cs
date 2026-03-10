using PxOperations.Domain.Exceptions;
using PxOperations.Domain.Projects;

namespace PxOperations.Domain.UnitTests;

public sealed class ProjectTests
{
    [Fact]
    public void Create_should_set_all_properties()
    {
        var project = Project.Create(
            DeliveryCenter.Dc1,
            ProjectStatus.InProgress,
            "Project Alpha",
            "Client A",
            ProjectType.Squad,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            "John Doe",
            RenewalStatus.Pending,
            "Awaiting approval");

        Assert.Equal(DeliveryCenter.Dc1, project.Dc);
        Assert.Equal(ProjectStatus.InProgress, project.Status);
        Assert.Equal("Project Alpha", project.Name);
        Assert.Equal("Client A", project.Client);
        Assert.Equal(ProjectType.Squad, project.Type);
        Assert.Equal(new DateOnly(2026, 1, 1), project.StartDate);
        Assert.Equal(new DateOnly(2026, 12, 31), project.EndDate);
        Assert.Equal("John Doe", project.DeliveryManager);
        Assert.Equal(RenewalStatus.Pending, project.Renewal);
        Assert.Equal("Awaiting approval", project.RenewalObservation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_should_fail_when_name_is_empty_or_whitespace(string? name)
    {
        var exception = Assert.Throws<BusinessRuleValidationException>(() =>
            Project.Create(
                DeliveryCenter.Dc1,
                ProjectStatus.InProgress,
                name!,
                null,
                ProjectType.Squad,
                null,
                null,
                null,
                RenewalStatus.None,
                null));

        Assert.Equal("Project name must not be empty.", exception.Message);
    }

    [Fact]
    public void Create_should_fail_when_end_date_precedes_start_date()
    {
        var exception = Assert.Throws<BusinessRuleValidationException>(() =>
            Project.Create(
                DeliveryCenter.Dc1,
                ProjectStatus.InProgress,
                "Project Beta",
                null,
                ProjectType.Squad,
                new DateOnly(2026, 6, 1),
                new DateOnly(2026, 1, 1),
                null,
                RenewalStatus.None,
                null));

        Assert.Equal("Project end date must not precede start date.", exception.Message);
    }

    [Fact]
    public void Create_should_accept_null_optional_fields()
    {
        var project = Project.Create(
            DeliveryCenter.Dc3,
            ProjectStatus.Scheduled,
            "Minimal Project",
            null,
            ProjectType.FixedScope,
            null,
            null,
            null,
            RenewalStatus.None,
            null);

        Assert.Null(project.Client);
        Assert.Null(project.StartDate);
        Assert.Null(project.EndDate);
        Assert.Null(project.DeliveryManager);
        Assert.Null(project.RenewalObservation);
    }

    [Fact]
    public void Update_should_modify_properties()
    {
        var project = Project.Create(
            DeliveryCenter.Dc1,
            ProjectStatus.InProgress,
            "Original",
            null,
            ProjectType.Squad,
            null,
            null,
            null,
            RenewalStatus.None,
            null);

        project.Update(
            DeliveryCenter.Dc5,
            ProjectStatus.Closed,
            "Updated",
            "New Client",
            ProjectType.Staffing,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 9, 30),
            "Jane Doe",
            RenewalStatus.Approved,
            "Renewed");

        Assert.Equal(DeliveryCenter.Dc5, project.Dc);
        Assert.Equal(ProjectStatus.Closed, project.Status);
        Assert.Equal("Updated", project.Name);
        Assert.Equal("New Client", project.Client);
        Assert.Equal(ProjectType.Staffing, project.Type);
        Assert.Equal(new DateOnly(2026, 3, 1), project.StartDate);
        Assert.Equal(new DateOnly(2026, 9, 30), project.EndDate);
        Assert.Equal("Jane Doe", project.DeliveryManager);
        Assert.Equal(RenewalStatus.Approved, project.Renewal);
        Assert.Equal("Renewed", project.RenewalObservation);
    }

    [Fact]
    public void Update_should_validate_business_rules()
    {
        var project = Project.Create(
            DeliveryCenter.Dc1,
            ProjectStatus.InProgress,
            "Valid Name",
            null,
            ProjectType.Squad,
            null,
            null,
            null,
            RenewalStatus.None,
            null);

        Assert.Throws<BusinessRuleValidationException>(() =>
            project.Update(
                DeliveryCenter.Dc1,
                ProjectStatus.InProgress,
                "",
                null,
                ProjectType.Squad,
                null,
                null,
                null,
                RenewalStatus.None,
                null));
    }
}
