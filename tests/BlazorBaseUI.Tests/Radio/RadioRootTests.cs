using BlazorBaseUI.RadioGroup;

namespace BlazorBaseUI.Tests.Radio;

public class RadioRootTests : BunitContext, IRadioRootContract
{
    public RadioRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupRadioModule(JSInterop);
    }

    private RenderFragment CreateRadioRoot(
        string value = "a",
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        string? name = null,
        Func<RadioRootState, string>? classValue = null,
        Func<RadioRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment<RenderProps<RadioRootState>>? render = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<RadioGroup<string>>(0);
            builder.AddAttribute(1, "DefaultValue", (string?)null);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(groupBuilder =>
            {
                groupBuilder.OpenComponent<RadioRoot<string>>(0);
                var attrIndex = 1;

                groupBuilder.AddAttribute(attrIndex++, "Value", value);
                if (disabled)
                    groupBuilder.AddAttribute(attrIndex++, "Disabled", true);
                if (readOnly)
                    groupBuilder.AddAttribute(attrIndex++, "ReadOnly", true);
                if (required)
                    groupBuilder.AddAttribute(attrIndex++, "Required", true);
                if (name is not null)
                    groupBuilder.AddAttribute(attrIndex++, "Name", name);
                if (classValue is not null)
                    groupBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (styleValue is not null)
                    groupBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    groupBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                if (render is not null)
                    groupBuilder.AddAttribute(attrIndex++, "Render", render);
                if (childContent is not null)
                    groupBuilder.AddAttribute(attrIndex++, "ChildContent", childContent);

                groupBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateRadioWithIndicator(
        string value = "a",
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        string? defaultValue = null,
        IReadOnlyDictionary<string, object>? indicatorAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<RadioGroup<string>>(0);
            if (defaultValue is not null)
                builder.AddAttribute(1, "DefaultValue", defaultValue);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(groupBuilder =>
            {
                groupBuilder.OpenComponent<RadioRoot<string>>(0);
                var attrIndex = 1;

                groupBuilder.AddAttribute(attrIndex++, "Value", value);
                if (disabled)
                    groupBuilder.AddAttribute(attrIndex++, "Disabled", true);
                if (readOnly)
                    groupBuilder.AddAttribute(attrIndex++, "ReadOnly", true);
                if (required)
                    groupBuilder.AddAttribute(attrIndex++, "Required", true);

                groupBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
                {
                    innerBuilder.OpenComponent<RadioIndicator>(0);
                    if (indicatorAttributes is not null)
                        innerBuilder.AddAttribute(1, "AdditionalAttributes", indicatorAttributes);
                    innerBuilder.CloseComponent();
                }));

                groupBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateRadioInGroup(
        string? defaultValue = null,
        string? controlledValue = null,
        bool groupDisabled = false,
        bool groupReadOnly = false,
        Action<RadioGroupValueChangeEventArgs<string>>? onValueChange = null,
        EventCallback<string?>? valueChanged = null)
    {
        return builder =>
        {
            builder.OpenComponent<RadioGroup<string>>(0);
            var attrIndex = 1;

            if (controlledValue is not null)
                builder.AddAttribute(attrIndex++, "Value", controlledValue);
            if (defaultValue is not null)
                builder.AddAttribute(attrIndex++, "DefaultValue", defaultValue);
            if (groupDisabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            if (groupReadOnly)
                builder.AddAttribute(attrIndex++, "ReadOnly", true);
            if (onValueChange is not null)
                builder.AddAttribute(attrIndex++, "OnValueChange", EventCallback.Factory.Create(this, onValueChange));
            if (valueChanged.HasValue)
                builder.AddAttribute(attrIndex++, "ValueChanged", valueChanged.Value);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(groupBuilder =>
            {
                groupBuilder.OpenComponent<RadioRoot<string>>(0);
                groupBuilder.AddAttribute(1, "Value", "a");
                groupBuilder.AddAttribute(2, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "radio-a" } });
                groupBuilder.CloseComponent();

                groupBuilder.OpenComponent<RadioRoot<string>>(10);
                groupBuilder.AddAttribute(11, "Value", "b");
                groupBuilder.AddAttribute(12, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "radio-b" } });
                groupBuilder.CloseComponent();

                groupBuilder.OpenComponent<RadioRoot<string>>(20);
                groupBuilder.AddAttribute(21, "Value", "c");
                groupBuilder.AddAttribute(22, "AdditionalAttributes",
                    (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { { "data-testid", "radio-c" } });
                groupBuilder.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    // Rendering tests
    [Fact]
    public Task RendersAsSpanByDefault()
    {
        var cut = Render(CreateRadioRoot());

        var radio = cut.Find("[role='radio']");
        radio.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<RadioRootState>> renderAsDiv = props => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateRadioRoot(render: renderAsDiv));

        var radio = cut.Find("[role='radio']");
        radio.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateRadioRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "radio-root" },
                { "aria-label", "Select option" }
            }
        ));

        var radio = cut.Find("[role='radio']");
        radio.GetAttribute("data-testid").ShouldBe("radio-root");
        radio.GetAttribute("aria-label").ShouldBe("Select option");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateRadioRoot(
            classValue: _ => "custom-radio"
        ));

        var radio = cut.Find("[role='radio']");
        radio.GetAttribute("class").ShouldContain("custom-radio");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateRadioRoot(
            styleValue: _ => "width: 20px"
        ));

        var radio = cut.Find("[role='radio']");
        radio.GetAttribute("style").ShouldContain("width: 20px");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateRadioRoot(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }
        ));

        var radio = cut.Find("[role='radio']");
        var classAttr = radio.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OverridesBuiltInAttributes()
    {
        var cut = Render(CreateRadioRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-custom", "test-value" },
                { "aria-label", "Custom label" }
            }
        ));

        var radio = cut.Find("[role='radio']");
        radio.GetAttribute("data-custom").ShouldBe("test-value");
        radio.GetAttribute("aria-label").ShouldBe("Custom label");

        return Task.CompletedTask;
    }

    // ARIA and role tests
    [Fact]
    public Task HasRoleRadio()
    {
        var cut = Render(CreateRadioRoot());

        var radio = cut.Find("[role='radio']");
        radio.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaCheckedFalseByDefault()
    {
        var cut = Render(CreateRadioRoot());

        var radio = cut.Find("[role='radio']");
        radio.GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaCheckedTrueWhenChecked()
    {
        var cut = Render(CreateRadioInGroup(defaultValue: "a"));

        var radio = cut.Find("[data-testid='radio-a']");
        radio.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaRequiredWhenRequired()
    {
        var cut = Render(CreateRadioRoot(required: true));

        var radio = cut.Find("[role='radio']");
        radio.GetAttribute("aria-required").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveAriaRequiredByDefault()
    {
        var cut = Render(CreateRadioRoot());

        var radio = cut.Find("[role='radio']");
        radio.HasAttribute("aria-required").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabindexZeroByDefault()
    {
        // In a group with no selection, first radio gets tabindex 0
        var cut = Render(CreateRadioInGroup());

        var radio = cut.Find("[data-testid='radio-a']");
        radio.GetAttribute("tabindex").ShouldBe("0");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabindexMinusOneWhenDisabled()
    {
        var cut = Render(CreateRadioRoot(disabled: true));

        var radio = cut.Find("[role='radio']");
        radio.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }

    // Disabled tests
    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateRadioRoot(disabled: true));

        var radio = cut.Find("[role='radio']");
        radio.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataDisabledByDefault()
    {
        var cut = Render(CreateRadioRoot());

        var radio = cut.Find("[role='radio']");
        radio.HasAttribute("data-disabled").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotChangeStateWhenClickedDisabled()
    {
        var callCount = 0;

        var cut = Render(CreateRadioInGroup(
            groupDisabled: true,
            onValueChange: _ => callCount++
        ));

        var inputs = cut.FindAll("input[type='radio']");
        inputs[0].Change("a");

        callCount.ShouldBe(0);

        return Task.CompletedTask;
    }

    // ReadOnly tests
    [Fact]
    public Task HasAriaReadonlyWhenReadOnly()
    {
        var cut = Render(CreateRadioRoot(readOnly: true));

        var radio = cut.Find("[role='radio']");
        radio.GetAttribute("aria-readonly").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveAriaReadonlyByDefault()
    {
        var cut = Render(CreateRadioRoot());

        var radio = cut.Find("[role='radio']");
        radio.HasAttribute("aria-readonly").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotChangeStateWhenClickedReadOnly()
    {
        var callCount = 0;

        var cut = Render(CreateRadioInGroup(
            groupReadOnly: true,
            onValueChange: _ => callCount++
        ));

        var inputs = cut.FindAll("input[type='radio']");
        inputs[0].Change("a");

        callCount.ShouldBe(0);

        return Task.CompletedTask;
    }

    // Value prop tests
    [Fact]
    public Task DoesNotForwardValuePropToRoot()
    {
        var cut = Render(CreateRadioRoot(value: "test-val"));

        var radio = cut.Find("[role='radio']");
        radio.HasAttribute("value").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AllowsNullValue()
    {
        // RadioRoot<string> with null-like value should render without error
        var cut = Render(builder =>
        {
            builder.OpenComponent<RadioGroup<string>>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(groupBuilder =>
            {
                groupBuilder.OpenComponent<RadioRoot<string>>(0);
                groupBuilder.AddAttribute(1, "Value", (string)null!);
                groupBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var radio = cut.Find("[role='radio']");
        radio.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    // Name and hidden input tests
    [Fact]
    public Task RendersHiddenRadioInput()
    {
        var cut = Render(CreateRadioRoot());

        var input = cut.Find("input[type='radio']");
        input.ShouldNotBeNull();
        input.GetAttribute("aria-hidden").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HiddenInputHasCorrectAttributes()
    {
        var cut = Render(CreateRadioRoot(disabled: true, required: true));

        var input = cut.Find("input[type='radio']");
        input.HasAttribute("disabled").ShouldBeTrue();
        input.HasAttribute("required").ShouldBeTrue();
        input.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InputHasId()
    {
        var cut = Render(CreateRadioRoot());

        var input = cut.Find("input[type='radio']");
        input.HasAttribute("id").ShouldBeTrue();
        input.GetAttribute("id").ShouldNotBeNullOrEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsNameOnInputOnly()
    {
        var cut = Render(CreateRadioRoot(name: "radio-name"));

        var radio = cut.Find("[role='radio']");
        var input = cut.Find("input[type='radio']");

        radio.HasAttribute("name").ShouldBeFalse();
        input.GetAttribute("name").ShouldBe("radio-name");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsValueOnHiddenInput()
    {
        var cut = Render(CreateRadioRoot(value: "radio-value"));

        var input = cut.Find("input[type='radio']");
        input.GetAttribute("value").ShouldBe("radio-value");

        return Task.CompletedTask;
    }

    // Style hooks (data attributes) tests
    [Fact]
    public Task HasDataCheckedWhenChecked()
    {
        var cut = Render(CreateRadioInGroup(defaultValue: "a"));

        var radio = cut.Find("[data-testid='radio-a']");
        radio.HasAttribute("data-checked").ShouldBeTrue();
        radio.HasAttribute("data-unchecked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataUncheckedWhenUnchecked()
    {
        var cut = Render(CreateRadioInGroup());

        var radio = cut.Find("[data-testid='radio-a']");
        radio.HasAttribute("data-unchecked").ShouldBeTrue();
        radio.HasAttribute("data-checked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled_StyleHook()
    {
        var cut = Render(CreateRadioRoot(disabled: true));

        var radio = cut.Find("[role='radio']");
        radio.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataReadonlyWhenReadOnly_StyleHook()
    {
        var cut = Render(CreateRadioRoot(readOnly: true));

        var radio = cut.Find("[role='radio']");
        radio.HasAttribute("data-readonly").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataRequiredWhenRequired_StyleHook()
    {
        var cut = Render(CreateRadioRoot(required: true));

        var radio = cut.Find("[role='radio']");
        radio.HasAttribute("data-required").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task PlacesStyleHooksOnRootAndIndicator()
    {
        var cut = Render(CreateRadioWithIndicator(
            value: "a",
            defaultValue: "a",
            disabled: true,
            readOnly: true,
            required: true,
            indicatorAttributes: new Dictionary<string, object> { { "data-testid", "indicator" } }
        ));

        var radio = cut.Find("[role='radio']");
        var indicator = cut.Find("[data-testid='indicator']");

        // Root should have all data attributes
        radio.HasAttribute("data-checked").ShouldBeTrue();
        radio.HasAttribute("data-disabled").ShouldBeTrue();
        radio.HasAttribute("data-readonly").ShouldBeTrue();
        radio.HasAttribute("data-required").ShouldBeTrue();

        // Indicator should have all data attributes
        indicator.HasAttribute("data-checked").ShouldBeTrue();
        indicator.HasAttribute("data-disabled").ShouldBeTrue();
        indicator.HasAttribute("data-readonly").ShouldBeTrue();
        indicator.HasAttribute("data-required").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DataCheckedTogglesOnSelection()
    {
        var cut = Render(CreateRadioInGroup());

        var radioA = cut.Find("[data-testid='radio-a']");
        radioA.HasAttribute("data-unchecked").ShouldBeTrue();
        radioA.HasAttribute("data-checked").ShouldBeFalse();

        // Simulate clicking radio A by triggering the input change
        var inputs = cut.FindAll("input[type='radio']");
        inputs[0].Change("a");

        radioA = cut.Find("[data-testid='radio-a']");
        radioA.HasAttribute("data-checked").ShouldBeTrue();
        radioA.HasAttribute("data-unchecked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    // Context tests
    [Fact]
    public Task CascadesContextToChildren()
    {
        RadioIndicatorState? capturedState = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<RadioGroup<string>>(0);
            builder.AddAttribute(1, "DefaultValue", "a");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(groupBuilder =>
            {
                groupBuilder.OpenComponent<RadioRoot<string>>(0);
                groupBuilder.AddAttribute(1, "Value", "a");
                groupBuilder.AddAttribute(2, "Disabled", true);
                groupBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
                {
                    innerBuilder.OpenComponent<RadioIndicator>(0);
                    innerBuilder.AddAttribute(1, "ClassValue", (Func<RadioIndicatorState, string>)(state =>
                    {
                        capturedState = state;
                        return "indicator-class";
                    }));
                    innerBuilder.CloseComponent();
                }));
                groupBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedState.ShouldNotBeNull();
        capturedState!.Checked.ShouldBeTrue();
        capturedState.Disabled.ShouldBeTrue();

        return Task.CompletedTask;
    }

    // Element reference tests
    [Fact]
    public Task ExposesElementReference()
    {
        RadioRoot<string>? component = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<RadioGroup<string>>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(groupBuilder =>
            {
                groupBuilder.OpenComponent<RadioRoot<string>>(0);
                groupBuilder.AddAttribute(1, "Value", "a");
                groupBuilder.AddComponentReferenceCapture(2, obj => component = (RadioRoot<string>)obj);
                groupBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        component.ShouldNotBeNull();
        cut.WaitForState(() => component!.Element.HasValue);
        component!.Element.HasValue.ShouldBeTrue();

        return Task.CompletedTask;
    }

    // ClassValue/StyleValue state tests
    [Fact]
    public Task ClassValueReceivesCorrectState()
    {
        RadioRootState? capturedState = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<RadioGroup<string>>(0);
            builder.AddAttribute(1, "DefaultValue", "a");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(groupBuilder =>
            {
                groupBuilder.OpenComponent<RadioRoot<string>>(0);
                groupBuilder.AddAttribute(1, "Value", "a");
                groupBuilder.AddAttribute(2, "Disabled", true);
                groupBuilder.AddAttribute(3, "ReadOnly", true);
                groupBuilder.AddAttribute(4, "Required", true);
                groupBuilder.AddAttribute(5, "ClassValue", (Func<RadioRootState, string>)(state =>
                {
                    capturedState = state;
                    return "test-class";
                }));
                groupBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedState.ShouldNotBeNull();
        capturedState!.Checked.ShouldBeTrue();
        capturedState.Disabled.ShouldBeTrue();
        capturedState.ReadOnly.ShouldBeTrue();
        capturedState.Required.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task StyleValueReceivesCorrectState()
    {
        RadioRootState? capturedState = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<RadioGroup<string>>(0);
            builder.AddAttribute(1, "DefaultValue", "a");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(groupBuilder =>
            {
                groupBuilder.OpenComponent<RadioRoot<string>>(0);
                groupBuilder.AddAttribute(1, "Value", "a");
                groupBuilder.AddAttribute(2, "StyleValue", (Func<RadioRootState, string>)(state =>
                {
                    capturedState = state;
                    return "color: blue";
                }));
                groupBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedState.ShouldNotBeNull();
        capturedState!.Checked.ShouldBeTrue();

        return Task.CompletedTask;
    }

    // Group integration tests
    [Fact]
    public Task InheritsDisabledFromGroup()
    {
        var cut = Render(CreateRadioInGroup(groupDisabled: true));

        var radioA = cut.Find("[data-testid='radio-a']");
        var radioB = cut.Find("[data-testid='radio-b']");
        var radioC = cut.Find("[data-testid='radio-c']");

        radioA.HasAttribute("data-disabled").ShouldBeTrue();
        radioB.HasAttribute("data-disabled").ShouldBeTrue();
        radioC.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsTabindexBasedOnGroupSelection()
    {
        var cut = Render(CreateRadioInGroup(defaultValue: "b"));

        var radioA = cut.Find("[data-testid='radio-a']");
        var radioB = cut.Find("[data-testid='radio-b']");
        var radioC = cut.Find("[data-testid='radio-c']");

        // Only the selected radio should have tabindex 0
        radioA.GetAttribute("tabindex").ShouldBe("-1");
        radioB.GetAttribute("tabindex").ShouldBe("0");
        radioC.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }

}
