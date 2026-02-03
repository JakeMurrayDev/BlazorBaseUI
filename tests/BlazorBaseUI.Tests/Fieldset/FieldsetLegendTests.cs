using BlazorBaseUI.Fieldset;
using BlazorBaseUI.Tests.Contracts.Fieldset;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tests.Fieldset;

public class FieldsetLegendTests : BunitContext, IFieldsetLegendContract
{
    public FieldsetLegendTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateFieldsetWithLegend(string? customLegendId = null)
    {
        return builder =>
        {
            builder.OpenComponent<FieldsetRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(fieldsetBuilder =>
            {
                fieldsetBuilder.OpenComponent<FieldsetLegend>(0);

                if (!string.IsNullOrEmpty(customLegendId))
                    fieldsetBuilder.AddAttribute(1, "id", customLegendId);

                fieldsetBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Legend text")));
                fieldsetBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateFieldsetWithLegend());
        // FieldsetLegend renders as div by default
        var legendDiv = cut.Find("fieldset div");
        legendDiv.ShouldNotBeNull();
        legendDiv.TagName.ShouldBe("DIV");
        legendDiv.TextContent.ShouldContain("Legend text");
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaLabelledByOnFieldsetAutomatically()
    {
        var cut = Render(CreateFieldsetWithLegend());
        var fieldset = cut.Find("fieldset");
        var legend = cut.Find("fieldset div");

        var ariaLabelledBy = fieldset.GetAttribute("aria-labelledby");
        var legendId = legend.GetAttribute("id");

        ariaLabelledBy.ShouldNotBeNullOrEmpty();
        legendId.ShouldNotBeNullOrEmpty();
        ariaLabelledBy.ShouldBe(legendId);
        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaLabelledByWithCustomId()
    {
        var cut = Render(CreateFieldsetWithLegend(customLegendId: "custom-legend-id"));
        var fieldset = cut.Find("fieldset");

        fieldset.GetAttribute("aria-labelledby").ShouldBe("custom-legend-id");
        return Task.CompletedTask;
    }
}
