using PxOperations.Domain.Exceptions;
using PxOperations.Domain.Milestones;

namespace PxOperations.Domain.UnitTests.Milestones;

public sealed class MilestoneTests
{
    [Fact]
    public void Create_should_set_all_properties()
    {
        var milestone = Milestone.Create(
            10,
            MilestoneType.Kickoff,
            "Kickoff projeto A",
            new DateOnly(2026, 3, 20),
            new TimeOnly(9, 30),
            "Sala 1");

        Assert.Equal(10, milestone.ProjectId);
        Assert.Equal(MilestoneType.Kickoff, milestone.Type);
        Assert.Equal("Kickoff projeto A", milestone.Title);
        Assert.Equal(new DateOnly(2026, 3, 20), milestone.Date);
        Assert.Equal(new TimeOnly(9, 30), milestone.Time);
        Assert.Equal("Sala 1", milestone.Notes);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_should_fail_when_title_is_empty(string? title)
    {
        var exception = Assert.Throws<BusinessRuleValidationException>(() =>
            Milestone.Create(1, MilestoneType.Other, title!, new DateOnly(2026, 3, 20), null, null));

        Assert.Equal("Milestone title must not be empty.", exception.Message);
    }

    [Fact]
    public void Update_should_modify_properties()
    {
        var milestone = Milestone.Create(1, MilestoneType.Other, "Original", new DateOnly(2026, 3, 20), null, null);

        milestone.Update(2, MilestoneType.FinalDelivery, "Entrega", new DateOnly(2026, 3, 25), new TimeOnly(14, 0), "Cliente confirmado");

        Assert.Equal(2, milestone.ProjectId);
        Assert.Equal(MilestoneType.FinalDelivery, milestone.Type);
        Assert.Equal("Entrega", milestone.Title);
        Assert.Equal(new DateOnly(2026, 3, 25), milestone.Date);
        Assert.Equal(new TimeOnly(14, 0), milestone.Time);
        Assert.Equal("Cliente confirmado", milestone.Notes);
    }
}
