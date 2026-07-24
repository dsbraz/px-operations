using Bunit;
using PxOperations.Ui.Components.DataDisplay;

namespace PxOperations.Ui.Tests.Components;

public sealed class BrqTagTests : BunitContext
{
    [Theory]
    [InlineData(BrqSize.Small, "tag--sm")]
    [InlineData(BrqSize.Medium, "tag--md")]
    [InlineData(BrqSize.Large, "tag--lg")]
    public void Tag_should_render_the_requested_size(BrqSize size, string expectedClass)
    {
        var cut = Render<BrqTag>(parameters => parameters
            .Add(component => component.Label, "DC1")
            .Add(component => component.Size, size));

        Assert.Contains(expectedClass, cut.Find(".tag").ClassList);
    }

    [Theory]
    [InlineData(BrqTagTone.Purple, "tag--purple")]
    [InlineData(BrqTagTone.Gray, "tag--gray")]
    [InlineData(BrqTagTone.Orange, "tag--orange")]
    [InlineData(BrqTagTone.Blue, "tag--blue")]
    [InlineData(BrqTagTone.Green, "tag--green")]
    public void Tag_should_render_the_requested_brand_tone(BrqTagTone tone, string expectedClass)
    {
        var cut = Render<BrqTag>(parameters => parameters
            .Add(component => component.Label, "Indicador")
            .Add(component => component.Tone, tone));

        Assert.Contains(expectedClass, cut.Find(".tag").ClassList);
    }
}
