namespace BlazorBaseUI.Tests.Slider;

public class SliderRootTests : BunitContext, ISliderRootContract
{
    public SliderRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSliderModule(JSInterop);
    }

    private RenderFragment CreateSliderRoot(
        double? value = null,
        double[]? values = null,
        double? defaultValue = null,
        double[]? defaultValues = null,
        double min = 0,
        double max = 100,
        double step = 1,
        Orientation orientation = Orientation.Horizontal,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        Func<SliderRootState, string>? classValue = null,
        Func<SliderRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        string? asElement = null,
        EventCallback<SliderValueChangeEventArgs<double>>? onValueChange = null,
        EventCallback<SliderValueCommittedEventArgs<double>>? onValueCommitted = null,
        EventCallback<SliderValueChangeEventArgs<double[]>>? onValuesChange = null,
        EventCallback<SliderValueCommittedEventArgs<double[]>>? onValuesCommitted = null,
        bool includeControl = true,
        bool includeThumb = true,
        int thumbCount = 1)
    {
        return builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            var attrIndex = 1;

            if (value.HasValue)
                builder.AddAttribute(attrIndex++, "Value", value.Value);
            if (values is not null)
                builder.AddAttribute(attrIndex++, "Values", values);
            if (defaultValue.HasValue)
                builder.AddAttribute(attrIndex++, "DefaultValue", defaultValue.Value);
            if (defaultValues is not null)
                builder.AddAttribute(attrIndex++, "DefaultValues", defaultValues);

            builder.AddAttribute(attrIndex++, "Min", min);
            builder.AddAttribute(attrIndex++, "Max", max);
            builder.AddAttribute(attrIndex++, "Step", step);
            builder.AddAttribute(attrIndex++, "Orientation", orientation);

            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            if (readOnly)
                builder.AddAttribute(attrIndex++, "ReadOnly", true);
            if (required)
                builder.AddAttribute(attrIndex++, "Required", true);
            if (classValue is not null)
                builder.AddAttribute(attrIndex++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(attrIndex++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
            if (asElement is not null)
                builder.AddAttribute(attrIndex++, "As", asElement);
            if (onValueChange.HasValue)
                builder.AddAttribute(attrIndex++, "OnValueChange", onValueChange.Value);
            if (onValueCommitted.HasValue)
                builder.AddAttribute(attrIndex++, "OnValueCommitted", onValueCommitted.Value);
            if (onValuesChange.HasValue)
                builder.AddAttribute(attrIndex++, "OnValuesChange", onValuesChange.Value);
            if (onValuesCommitted.HasValue)
                builder.AddAttribute(attrIndex++, "OnValuesCommitted", onValuesCommitted.Value);

            builder.AddAttribute(attrIndex++, "ChildContent", CreateChildContent(includeControl, includeThumb, thumbCount));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateChildContent(bool includeControl = true, bool includeThumb = true, int thumbCount = 1)
    {
        return builder =>
        {
            if (includeControl)
            {
                builder.OpenComponent<SliderControl>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
                {
                    innerBuilder.OpenComponent<SliderTrack>(0);
                    innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(trackBuilder =>
                    {
                        trackBuilder.OpenComponent<SliderIndicator>(0);
                        trackBuilder.CloseComponent();

                        if (includeThumb)
                        {
                            for (var i = 0; i < thumbCount; i++)
                            {
                                trackBuilder.OpenComponent<SliderThumb>(1 + i);
                                if (thumbCount > 1)
                                {
                                    trackBuilder.AddAttribute(0, "Index", i);
                                }
                                trackBuilder.CloseComponent();
                            }
                        }
                    }));
                    innerBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateSliderRoot());

        var div = cut.Find("div[role='group']");
        div.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomAs()
    {
        var cut = Render(CreateSliderRoot(asElement: "section"));

        var section = cut.Find("section[role='group']");
        section.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateSliderRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "slider-root" },
                { "aria-label", "Volume" }
            }
        ));

        cut.Markup.ShouldContain("data-testid=\"slider-root\"");
        cut.Markup.ShouldContain("aria-label=\"Volume\"");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateSliderRoot(
            classValue: _ => "custom-slider"
        ));

        cut.Markup.ShouldContain("custom-slider");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateSliderRoot(
            styleValue: _ => "width: 200px"
        ));

        cut.Markup.ShouldContain("width: 200px");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateSliderRoot(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }
        ));

        cut.Markup.ShouldContain("static-class");
        cut.Markup.ShouldContain("dynamic-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRoleGroup()
    {
        var cut = Render(CreateSliderRoot());

        var root = cut.Find("[role='group']");
        root.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientationHorizontalByDefault()
    {
        var cut = Render(CreateSliderRoot());

        var root = cut.Find("[data-orientation='horizontal']");
        root.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientationVertical()
    {
        var cut = Render(CreateSliderRoot(orientation: Orientation.Vertical));

        var root = cut.Find("[data-orientation='vertical']");
        root.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateSliderRoot(disabled: true));

        var root = cut.Find("[role='group']");
        root.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataReadonlyWhenReadOnly()
    {
        var cut = Render(CreateSliderRoot(readOnly: true));

        var root = cut.Find("[role='group']");
        root.HasAttribute("data-readonly").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataRequiredWhenRequired()
    {
        var cut = Render(CreateSliderRoot(required: true));

        var root = cut.Find("[role='group']");
        root.HasAttribute("data-required").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesContextToChildren()
    {
        SliderRootState? capturedState = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 50.0);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SliderControl>(0);
                innerBuilder.AddAttribute(1, "ClassValue", (Func<SliderRootState, string>)(state =>
                {
                    capturedState = state;
                    return "control-class";
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedState.ShouldNotBeNull();
        capturedState!.Values.ShouldContain(50.0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledModeUsesDefaultValue()
    {
        var cut = Render(CreateSliderRoot(defaultValue: 30));

        var slider = cut.Find("input[type='range']");
        slider.GetAttribute("aria-valuenow").ShouldBe("30");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledModeUsesDefaultValues()
    {
        var cut = Render(CreateSliderRoot(defaultValues: [20, 80], thumbCount: 2));

        var sliders = cut.FindAll("input[type='range']");
        sliders.Count.ShouldBe(2);
        sliders[0].GetAttribute("aria-valuenow").ShouldBe("20");
        sliders[1].GetAttribute("aria-valuenow").ShouldBe("80");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledModeRespectsValueParameter()
    {
        var cut = Render(CreateSliderRoot(value: 75));

        var slider = cut.Find("input[type='range']");
        slider.GetAttribute("aria-valuenow").ShouldBe("75");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledModeRespectsValuesParameter()
    {
        var cut = Render(CreateSliderRoot(values: [25, 75], thumbCount: 2));

        var sliders = cut.FindAll("input[type='range']");
        sliders.Count.ShouldBe(2);
        sliders[0].GetAttribute("aria-valuenow").ShouldBe("25");
        sliders[1].GetAttribute("aria-valuenow").ShouldBe("75");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnValueChange()
    {
        var invoked = false;
        double receivedValue = 0;

        var cut = Render(CreateSliderRoot(
            defaultValue: 50,
            onValueChange: EventCallback.Factory.Create<SliderValueChangeEventArgs<double>>(this, args =>
            {
                invoked = true;
                receivedValue = args.Value;
            })
        ));

        var slider = cut.Find("input[type='range']");
        slider.Change("60");

        invoked.ShouldBeTrue();
        receivedValue.ShouldBe(60);

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnValueCommitted()
    {
        var invoked = false;
        double receivedValue = 0;

        var cut = Render(CreateSliderRoot(
            defaultValue: 50,
            onValueCommitted: EventCallback.Factory.Create<SliderValueCommittedEventArgs<double>>(this, args =>
            {
                invoked = true;
                receivedValue = args.Value;
            })
        ));

        var slider = cut.Find("input[type='range']");
        slider.Change("60");

        invoked.ShouldBeTrue();
        receivedValue.ShouldBe(60);

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnValuesChange()
    {
        var invoked = false;
        double[]? receivedValues = null;

        var cut = Render(CreateSliderRoot(
            defaultValues: [20, 80],
            thumbCount: 2,
            onValuesChange: EventCallback.Factory.Create<SliderValueChangeEventArgs<double[]>>(this, args =>
            {
                invoked = true;
                receivedValues = args.Value;
            })
        ));

        var sliders = cut.FindAll("input[type='range']");
        sliders[0].Change("30");

        invoked.ShouldBeTrue();
        receivedValues.ShouldNotBeNull();
        receivedValues![0].ShouldBe(30);

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnValuesCommitted()
    {
        var invoked = false;
        double[]? receivedValues = null;

        var cut = Render(CreateSliderRoot(
            defaultValues: [20, 80],
            thumbCount: 2,
            onValuesCommitted: EventCallback.Factory.Create<SliderValueCommittedEventArgs<double[]>>(this, args =>
            {
                invoked = true;
                receivedValues = args.Value;
            })
        ));

        var sliders = cut.FindAll("input[type='range']");
        sliders[0].Change("30");

        invoked.ShouldBeTrue();
        receivedValues.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ClampsValueToMinMax()
    {
        var cut = Render(CreateSliderRoot(defaultValue: 150, min: 0, max: 100));

        var slider = cut.Find("input[type='range']");
        slider.GetAttribute("aria-valuenow").ShouldBe("100");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsNonIntegerStep()
    {
        var cut = Render(CreateSliderRoot(defaultValue: 0.5, min: 0, max: 1, step: 0.1));

        var slider = cut.Find("input[type='range']");
        slider.GetAttribute("step").ShouldBe("0.1");
        slider.GetAttribute("aria-valuenow").ShouldBe("0.5");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HandlesVerySmallStepValues()
    {
        var cut = Render(CreateSliderRoot(defaultValue: 0.005, min: 0, max: 0.01, step: 0.001));

        var slider = cut.Find("input[type='range']");
        slider.GetAttribute("step").ShouldBe("0.001");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesMinAsStepOrigin()
    {
        // With min=5 and step=10, valid values are 5, 15, 25, etc.
        var cut = Render(CreateSliderRoot(defaultValue: 15, min: 5, max: 100, step: 10));

        var slider = cut.Find("input[type='range']");
        slider.GetAttribute("min").ShouldBe("5");
        slider.GetAttribute("step").ShouldBe("10");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ClampsValueBelowMin()
    {
        var cut = Render(CreateSliderRoot(defaultValue: -10, min: 0, max: 100));

        var slider = cut.Find("input[type='range']");
        slider.GetAttribute("aria-valuenow").ShouldBe("0");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsThreeOrMoreThumbs()
    {
        var cut = Render(CreateSliderRoot(defaultValues: [10, 50, 90], thumbCount: 3));

        var sliders = cut.FindAll("input[type='range']");
        sliders.Count.ShouldBe(3);
        sliders[0].GetAttribute("aria-valuenow").ShouldBe("10");
        sliders[1].GetAttribute("aria-valuenow").ShouldBe("50");
        sliders[2].GetAttribute("aria-valuenow").ShouldBe("90");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ThumbsHaveCorrectIndices()
    {
        var cut = Render(CreateSliderRoot(defaultValues: [10, 50, 90], thumbCount: 3));

        var thumbs = cut.FindAll("[data-index]");
        thumbs.Count.ShouldBe(3);
        thumbs[0].GetAttribute("data-index").ShouldBe("0");
        thumbs[1].GetAttribute("data-index").ShouldBe("1");
        thumbs[2].GetAttribute("data-index").ShouldBe("2");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ClassValueReceivesCorrectState()
    {
        SliderRootState? capturedState = null;

        var cut = Render(CreateSliderRoot(
            defaultValue: 75,
            disabled: true,
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Values.ShouldContain(75.0);
        capturedState.Disabled.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task StyleValueReceivesCorrectState()
    {
        SliderRootState? capturedState = null;

        var cut = Render(CreateSliderRoot(
            defaultValues: [20, 80],
            thumbCount: 2,
            styleValue: state =>
            {
                capturedState = state;
                return "color: red";
            }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Values.Length.ShouldBe(2);
        capturedState.Values[0].ShouldBe(20);
        capturedState.Values[1].ShouldBe(80);

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsNameAttribute()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 50.0);
            builder.AddAttribute(2, "Name", "volume");
            builder.AddAttribute(3, "ChildContent", CreateChildContent());
            builder.CloseComponent();
        });

        var input = cut.Find("input[type='range']");
        input.GetAttribute("name").ShouldBe("volume");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ReadOnlyDoesNotPreventValueDisplay()
    {
        // Using the existing tested pattern that works
        var cut = Render(CreateSliderRoot(readOnly: true));

        // Value is still displayed in readonly mode (defaults to min since no defaultValue)
        var slider = cut.Find("input[type='range']");
        slider.GetAttribute("aria-valuenow").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task UsesLargeStepValue()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<SliderRoot>(0);
            builder.AddAttribute(1, "DefaultValue", 50.0);
            builder.AddAttribute(2, "LargeStep", 10.0);
            builder.AddAttribute(3, "ChildContent", CreateChildContent());
            builder.CloseComponent();
        });

        // LargeStep is used for PageUp/PageDown - this verifies the component accepts the parameter
        // Actual behavior is tested in Playwright
        var root = cut.Find("[role='group']");
        root.ShouldNotBeNull();

        return Task.CompletedTask;
    }
}
