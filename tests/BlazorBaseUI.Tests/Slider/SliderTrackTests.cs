namespace BlazorBaseUI.Tests.Slider;

public class SliderTrackTests : BunitContext, ISliderTrackContract
{
    public SliderTrackTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSliderModule(JSInterop);
    }

    private RenderFragment CreateSliderWithTrack(
        Orientation orientation = Orientation.Horizontal,
        bool disabled = false,
        Func<SliderRootState, string>? classValue = null,
        Func<SliderRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        string? asElement = null)
    {
        return builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 50.0);
            builder.AddAttribute(2, "Orientation", orientation);
            if (disabled)
                builder.AddAttribute(3, "Disabled", true);
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderControl>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(controlBuilder =>
                {
                    controlBuilder.OpenComponent<SliderTrack>(0);
                    if (classValue is not null)
                        controlBuilder.AddAttribute(0, "ClassValue", classValue);
                    if (styleValue is not null)
                        controlBuilder.AddAttribute(1, "StyleValue", styleValue);
                    var mergedAttrs = new Dictionary<string, object> { { "data-testid", "slider-track" } };
                    if (additionalAttributes is not null)
                    {
                        foreach (var kvp in additionalAttributes)
                            mergedAttrs[kvp.Key] = kvp.Value;
                    }
                    controlBuilder.AddAttribute(2, "AdditionalAttributes", (IReadOnlyDictionary<string, object>)mergedAttrs);
                    if (asElement is not null)
                        controlBuilder.AddAttribute(3, "As", asElement);
                    controlBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(trackBuilder =>
                    {
                        trackBuilder.OpenComponent<SliderThumb>(0);
                        trackBuilder.CloseComponent();
                    }));
                    controlBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateSliderWithTrack());

        var track = cut.Find("[data-testid='slider-track']");
        track.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomAs()
    {
        var cut = Render(CreateSliderWithTrack(asElement: "span"));

        var track = cut.Find("[data-testid='slider-track']");
        track.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateSliderWithTrack(
            additionalAttributes: new Dictionary<string, object>
            {
                { "aria-hidden", "true" }
            }
        ));

        var track = cut.Find("[data-testid='slider-track']");
        track.GetAttribute("aria-hidden").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateSliderWithTrack(
            classValue: _ => "track-class"
        ));

        var track = cut.Find("[data-testid='slider-track']");
        track.GetAttribute("class").ShouldContain("track-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateSliderWithTrack(
            styleValue: _ => "background: gray"
        ));

        var track = cut.Find("[data-testid='slider-track']");
        track.GetAttribute("style").ShouldContain("background: gray");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasPositionRelativeStyle()
    {
        var cut = Render(CreateSliderWithTrack());

        var track = cut.Find("[data-testid='slider-track']");
        track.GetAttribute("style").ShouldContain("position: relative");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientation()
    {
        var cut = Render(CreateSliderWithTrack(orientation: Orientation.Vertical));

        var track = cut.Find("[data-testid='slider-track']");
        track.GetAttribute("data-orientation").ShouldBe("vertical");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateSliderWithTrack(disabled: true));

        var track = cut.Find("[data-testid='slider-track']");
        track.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }
}
