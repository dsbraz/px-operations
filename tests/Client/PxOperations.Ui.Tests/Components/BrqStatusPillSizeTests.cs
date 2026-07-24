using Bunit;
using PxOperations.Ui.Components.DataDisplay;

namespace PxOperations.Ui.Tests.Components;

public sealed class BrqStatusPillSizeTests : BunitContext
{
    [Theory]
    [InlineData(BrqSize.Small, "pill--sm")]
    [InlineData(BrqSize.Medium, "pill--md")]
    [InlineData(BrqSize.Large, "pill--lg")]
    public void Pill_should_render_the_requested_size(BrqSize size, string expectedClass)
    {
        var cut = Render<BrqStatusPill>(parameters => parameters
            .Add(component => component.Label, "Em andamento")
            .Add(component => component.Size, size));

        Assert.Contains(expectedClass, cut.Find(".pill").ClassList);
    }
}
