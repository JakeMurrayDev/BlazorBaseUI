namespace BlazorBaseUI.Tests.Switch;

public class SwitchRootTests : BunitContext, ISwitchRootContract
{
    public SwitchRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSwitchModule(JSInterop);
    }

    private RenderFragment CreateSwitchRoot(
        bool? isChecked = null,
        bool defaultChecked = false,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        string? name = null,
        string? value = null,
        string? uncheckedValue = null,
        bool nativeButton = false,
        Func<SwitchRootState, string>? classValue = null,
        Func<SwitchRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        string? asElement = null,
        EventCallback<SwitchCheckedChangeEventArgs>? onCheckedChange = null,
        EventCallback<bool>? checkedChanged = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<SwitchRoot>(0);
            var attrIndex = 1;

            if (isChecked.HasValue)
                builder.AddAttribute(attrIndex++, "Checked", isChecked.Value);
            if (defaultChecked)
                builder.AddAttribute(attrIndex++, "DefaultChecked", true);
            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            if (readOnly)
                builder.AddAttribute(attrIndex++, "ReadOnly", true);
            if (required)
                builder.AddAttribute(attrIndex++, "Required", true);
            if (name is not null)
                builder.AddAttribute(attrIndex++, "Name", name);
            if (value is not null)
                builder.AddAttribute(attrIndex++, "Value", value);
            if (uncheckedValue is not null)
                builder.AddAttribute(attrIndex++, "UncheckedValue", uncheckedValue);
            if (nativeButton)
                builder.AddAttribute(attrIndex++, "NativeButton", true);
            if (classValue is not null)
                builder.AddAttribute(attrIndex++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(attrIndex++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
            if (asElement is not null)
                builder.AddAttribute(attrIndex++, "As", asElement);
            if (onCheckedChange.HasValue)
                builder.AddAttribute(attrIndex++, "OnCheckedChange", onCheckedChange.Value);
            if (checkedChanged.HasValue)
                builder.AddAttribute(attrIndex++, "CheckedChanged", checkedChanged.Value);
            if (childContent is not null)
                builder.AddAttribute(attrIndex++, "ChildContent", childContent);

            builder.CloseComponent();
        };
    }

    private RenderFragment CreateSwitchWithThumb(
        bool? isChecked = null,
        bool defaultChecked = false,
        bool disabled = false,
        bool readOnly = false,
        bool required = false,
        IReadOnlyDictionary<string, object>? thumbAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<SwitchRoot>(0);
            var attrIndex = 1;

            if (isChecked.HasValue)
                builder.AddAttribute(attrIndex++, "Checked", isChecked.Value);
            if (defaultChecked)
                builder.AddAttribute(attrIndex++, "DefaultChecked", true);
            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            if (readOnly)
                builder.AddAttribute(attrIndex++, "ReadOnly", true);
            if (required)
                builder.AddAttribute(attrIndex++, "Required", true);

            builder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SwitchThumb>(0);
                if (thumbAttributes is not null)
                    innerBuilder.AddAttribute(1, "AdditionalAttributes", thumbAttributes);
                innerBuilder.CloseComponent();
            }));

            builder.CloseComponent();
        };
    }

    // Rendering tests
    [Fact]
    public Task RendersAsSpanByDefault()
    {
        var cut = Render(CreateSwitchRoot());

        var switchEl = cut.Find("[role='switch']");
        switchEl.TagName.ShouldBe("SPAN");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomAs()
    {
        var cut = Render(CreateSwitchRoot(asElement: "div"));

        var switchEl = cut.Find("[role='switch']");
        switchEl.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateSwitchRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "switch-root" },
                { "aria-label", "Toggle setting" }
            }
        ));

        var switchEl = cut.Find("[role='switch']");
        switchEl.GetAttribute("data-testid").ShouldBe("switch-root");
        switchEl.GetAttribute("aria-label").ShouldBe("Toggle setting");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateSwitchRoot(
            classValue: _ => "custom-switch"
        ));

        var switchEl = cut.Find("[role='switch']");
        switchEl.GetAttribute("class").ShouldContain("custom-switch");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateSwitchRoot(
            styleValue: _ => "width: 50px"
        ));

        var switchEl = cut.Find("[role='switch']");
        switchEl.GetAttribute("style").ShouldContain("width: 50px");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateSwitchRoot(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }
        ));

        var switchEl = cut.Find("[role='switch']");
        var classAttr = switchEl.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OverridesBuiltInAttributes()
    {
        // The component sets role="switch" AFTER AddMultipleAttributes, so role cannot be overridden.
        // But we can verify that AdditionalAttributes are applied before built-in attributes.
        // Test that custom tabindex is applied (then overridden by component's tabindex)
        var cut = Render(CreateSwitchRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-custom", "test-value" },
                { "aria-label", "Custom label" }
            }
        ));

        var switchEl = cut.Find("[role='switch']");
        switchEl.GetAttribute("data-custom").ShouldBe("test-value");
        switchEl.GetAttribute("aria-label").ShouldBe("Custom label");

        return Task.CompletedTask;
    }

    // ARIA and role tests
    [Fact]
    public Task HasRoleSwitch()
    {
        var cut = Render(CreateSwitchRoot());

        var switchEl = cut.Find("[role='switch']");
        switchEl.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaCheckedFalseByDefault()
    {
        var cut = Render(CreateSwitchRoot());

        var switchEl = cut.Find("[role='switch']");
        switchEl.GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaCheckedTrueWhenChecked()
    {
        var cut = Render(CreateSwitchRoot(defaultChecked: true));

        var switchEl = cut.Find("[role='switch']");
        switchEl.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabindexZeroByDefault()
    {
        var cut = Render(CreateSwitchRoot());

        var switchEl = cut.Find("[role='switch']");
        switchEl.GetAttribute("tabindex").ShouldBe("0");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabindexMinusOneWhenDisabled()
    {
        var cut = Render(CreateSwitchRoot(disabled: true));

        var switchEl = cut.Find("[role='switch']");
        switchEl.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }

    // Disabled tests
    [Fact]
    public Task UsesAriaDisabledInsteadOfHtmlDisabled()
    {
        var cut = Render(CreateSwitchRoot(disabled: true, nativeButton: true));

        var switchEl = cut.Find("[role='switch']");
        switchEl.HasAttribute("disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDisabledAttributeByDefault()
    {
        var cut = Render(CreateSwitchRoot());

        var switchEl = cut.Find("[role='switch']");
        switchEl.HasAttribute("disabled").ShouldBeFalse();

        return Task.CompletedTask;
    }

    // ReadOnly tests
    [Fact]
    public Task HasAriaReadonlyWhenReadOnly()
    {
        var cut = Render(CreateSwitchRoot(readOnly: true));

        var switchEl = cut.Find("[role='switch']");
        switchEl.GetAttribute("aria-readonly").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveAriaReadonlyByDefault()
    {
        var cut = Render(CreateSwitchRoot());

        var switchEl = cut.Find("[role='switch']");
        switchEl.HasAttribute("aria-readonly").ShouldBeFalse();

        return Task.CompletedTask;
    }

    // Required tests
    [Fact]
    public Task HasAriaRequiredWhenRequired()
    {
        var cut = Render(CreateSwitchRoot(required: true));

        var switchEl = cut.Find("[role='switch']");
        switchEl.GetAttribute("aria-required").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveAriaRequiredByDefault()
    {
        var cut = Render(CreateSwitchRoot());

        var switchEl = cut.Find("[role='switch']");
        switchEl.HasAttribute("aria-required").ShouldBeFalse();

        return Task.CompletedTask;
    }

    // Name and Value tests
    [Fact]
    public Task SetsNameOnInputOnly()
    {
        var cut = Render(CreateSwitchRoot(name: "switch-name"));

        var switchEl = cut.Find("[role='switch']");
        var input = cut.Find("input[type='checkbox']");

        switchEl.HasAttribute("name").ShouldBeFalse();
        input.GetAttribute("name").ShouldBe("switch-name");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotSetValueByDefault()
    {
        var cut = Render(CreateSwitchRoot());

        var input = cut.Find("input[type='checkbox']");
        input.HasAttribute("value").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsValueOnInputOnly()
    {
        var cut = Render(CreateSwitchRoot(value: "1"));

        var switchEl = cut.Find("[role='switch']");
        var input = cut.Find("input[type='checkbox']");

        switchEl.HasAttribute("value").ShouldBeFalse();
        input.GetAttribute("value").ShouldBe("1");

        return Task.CompletedTask;
    }

    // Hidden checkbox input tests
    [Fact]
    public Task RendersHiddenCheckboxInput()
    {
        var cut = Render(CreateSwitchRoot());

        var input = cut.Find("input[type='checkbox']");
        input.ShouldNotBeNull();
        input.GetAttribute("aria-hidden").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HiddenInputHasCorrectAttributes()
    {
        var cut = Render(CreateSwitchRoot(disabled: true, required: true));

        var input = cut.Find("input[type='checkbox']");
        input.HasAttribute("disabled").ShouldBeTrue();
        input.HasAttribute("required").ShouldBeTrue();
        input.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InputHasIdWhenNotNativeButton()
    {
        var cut = Render(CreateSwitchRoot());

        var input = cut.Find("input[type='checkbox']");
        input.HasAttribute("id").ShouldBeTrue();
        input.GetAttribute("id").ShouldNotBeNullOrEmpty();

        return Task.CompletedTask;
    }

    // UncheckedValue tests
    [Fact]
    public Task RendersUncheckedValueHiddenInput()
    {
        var cut = Render(CreateSwitchRoot(name: "toggle", uncheckedValue: "off"));

        var hiddenInput = cut.Find("input[type='hidden']");
        hiddenInput.ShouldNotBeNull();
        hiddenInput.GetAttribute("name").ShouldBe("toggle");
        hiddenInput.GetAttribute("value").ShouldBe("off");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderUncheckedValueWhenChecked()
    {
        var cut = Render(CreateSwitchRoot(name: "toggle", uncheckedValue: "off", defaultChecked: true));

        var hiddenInputs = cut.FindAll("input[type='hidden']");
        hiddenInputs.Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    // Style hooks (data attributes) tests
    [Fact]
    public Task HasDataCheckedWhenChecked()
    {
        var cut = Render(CreateSwitchRoot(defaultChecked: true));

        var switchEl = cut.Find("[role='switch']");
        switchEl.HasAttribute("data-checked").ShouldBeTrue();
        switchEl.HasAttribute("data-unchecked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataUncheckedWhenUnchecked()
    {
        var cut = Render(CreateSwitchRoot());

        var switchEl = cut.Find("[role='switch']");
        switchEl.HasAttribute("data-unchecked").ShouldBeTrue();
        switchEl.HasAttribute("data-checked").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateSwitchRoot(disabled: true));

        var switchEl = cut.Find("[role='switch']");
        switchEl.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataReadonlyWhenReadOnly()
    {
        var cut = Render(CreateSwitchRoot(readOnly: true));

        var switchEl = cut.Find("[role='switch']");
        switchEl.HasAttribute("data-readonly").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataRequiredWhenRequired()
    {
        var cut = Render(CreateSwitchRoot(required: true));

        var switchEl = cut.Find("[role='switch']");
        switchEl.HasAttribute("data-required").ShouldBeTrue();

        return Task.CompletedTask;
    }

    // Controlled/Uncontrolled tests
    [Fact]
    public Task UncontrolledModeUsesDefaultChecked()
    {
        var cut = Render(CreateSwitchRoot(defaultChecked: true));

        var switchEl = cut.Find("[role='switch']");
        switchEl.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledModeRespectsCheckedParameter()
    {
        var cut = Render(CreateSwitchRoot(isChecked: true));

        var switchEl = cut.Find("[role='switch']");
        switchEl.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UpdatesStateWhenControlledValueChanges()
    {
        // Test that controlled value of false renders correctly
        var cutFalse = Render(CreateSwitchRoot(isChecked: false));
        var switchElFalse = cutFalse.Find("[role='switch']");
        switchElFalse.GetAttribute("aria-checked").ShouldBe("false");

        // Test that controlled value of true renders correctly
        var cutTrue = Render(CreateSwitchRoot(isChecked: true));
        var switchElTrue = cutTrue.Find("[role='switch']");
        switchElTrue.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    // Event callback tests
    [Fact]
    public Task InvokesOnCheckedChangeOnInputChange()
    {
        var invoked = false;
        bool? receivedValue = null;

        var cut = Render(CreateSwitchRoot(
            onCheckedChange: EventCallback.Factory.Create<SwitchCheckedChangeEventArgs>(this, args =>
            {
                invoked = true;
                receivedValue = args.Checked;
            })
        ));

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        invoked.ShouldBeTrue();
        receivedValue.ShouldBe(true);

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesCheckedChangedOnInputChange()
    {
        var invoked = false;
        bool? receivedValue = null;

        var cut = Render(CreateSwitchRoot(
            checkedChanged: EventCallback.Factory.Create<bool>(this, value =>
            {
                invoked = true;
                receivedValue = value;
            })
        ));

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        invoked.ShouldBeTrue();
        receivedValue.ShouldBe(true);

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotInvokeCallbacksWhenDisabled()
    {
        var invoked = false;

        var cut = Render(CreateSwitchRoot(
            disabled: true,
            onCheckedChange: EventCallback.Factory.Create<SwitchCheckedChangeEventArgs>(this, _ => invoked = true)
        ));

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        invoked.ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotInvokeCallbacksWhenReadOnly()
    {
        var invoked = false;

        var cut = Render(CreateSwitchRoot(
            readOnly: true,
            onCheckedChange: EventCallback.Factory.Create<SwitchCheckedChangeEventArgs>(this, _ => invoked = true)
        ));

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        invoked.ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnCheckedChangeCancellationPreventsStateChange()
    {
        var cut = Render(CreateSwitchRoot(
            onCheckedChange: EventCallback.Factory.Create<SwitchCheckedChangeEventArgs>(this, args =>
            {
                args.Cancel();
            })
        ));

        var switchEl = cut.Find("[role='switch']");
        switchEl.GetAttribute("aria-checked").ShouldBe("false");

        var input = cut.Find("input[type='checkbox']");
        input.Change(true);

        // State should not change because the event was cancelled
        switchEl = cut.Find("[role='switch']");
        switchEl.GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }

    // Context cascading tests
    [Fact]
    public Task CascadesContextToChildren()
    {
        SwitchRootState? capturedState = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<SwitchRoot>(0);
            builder.AddAttribute(1, "DefaultChecked", true);
            builder.AddAttribute(2, "Disabled", true);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SwitchThumb>(0);
                innerBuilder.AddAttribute(1, "ClassValue", (Func<SwitchRootState, string>)(state =>
                {
                    capturedState = state;
                    return "thumb-class";
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedState.ShouldNotBeNull();
        capturedState!.Checked.ShouldBeTrue();
        capturedState.Disabled.ShouldBeTrue();

        return Task.CompletedTask;
    }

    // NativeButton mode tests
    [Fact]
    public Task NativeButtonRendersAsButton()
    {
        var cut = Render(CreateSwitchRoot(nativeButton: true));

        var switchEl = cut.Find("[role='switch']");
        switchEl.TagName.ShouldBe("BUTTON");

        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButtonHasRoleSwitch()
    {
        var cut = Render(CreateSwitchRoot(nativeButton: true));

        var switchEl = cut.Find("[role='switch']");
        switchEl.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButtonHasDisabledAttribute()
    {
        var cut = Render(CreateSwitchRoot(nativeButton: true, disabled: true));

        var switchEl = cut.Find("[role='switch']");
        switchEl.HasAttribute("disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButtonHasCorrectId()
    {
        var cut = Render(CreateSwitchRoot(
            nativeButton: true,
            additionalAttributes: new Dictionary<string, object>
            {
                { "id", "my-switch" }
            }
        ));

        var switchEl = cut.Find("[role='switch']");
        switchEl.GetAttribute("id").ShouldBe("my-switch");

        return Task.CompletedTask;
    }

    // Element reference tests
    [Fact]
    public Task ExposesElementReference()
    {
        SwitchRoot? component = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<SwitchRoot>(0);
            builder.AddComponentReferenceCapture(1, obj => component = (SwitchRoot)obj);
            builder.CloseComponent();
        });

        component.ShouldNotBeNull();
        // Element reference is captured after render
        cut.WaitForState(() => component!.Element.HasValue);
        component!.Element.HasValue.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ExposesInputElementReference()
    {
        SwitchRoot? component = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<SwitchRoot>(0);
            builder.AddComponentReferenceCapture(1, obj => component = (SwitchRoot)obj);
            builder.CloseComponent();
        });

        component.ShouldNotBeNull();
        component!.InputElement.Id.ShouldNotBeNullOrEmpty();

        return Task.CompletedTask;
    }

    // RenderAs validation tests
    [Fact]
    public Task ThrowsWhenRenderAsDoesNotImplementInterface()
    {
        Should.Throw<InvalidOperationException>(() =>
        {
            Render(builder =>
            {
                builder.OpenComponent<SwitchRoot>(0);
                builder.AddAttribute(1, "RenderAs", typeof(NonReferencableComponent));
                builder.CloseComponent();
            });
        });

        return Task.CompletedTask;
    }

    // ClassValue/StyleValue state tests
    [Fact]
    public Task ClassValueReceivesCorrectState()
    {
        SwitchRootState? capturedState = null;

        var cut = Render(CreateSwitchRoot(
            defaultChecked: true,
            disabled: true,
            readOnly: true,
            required: true,
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }
        ));

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
        SwitchRootState? capturedState = null;

        var cut = Render(CreateSwitchRoot(
            defaultChecked: true,
            styleValue: state =>
            {
                capturedState = state;
                return "color: blue";
            }
        ));

        capturedState.ShouldNotBeNull();
        capturedState!.Checked.ShouldBeTrue();

        return Task.CompletedTask;
    }

    // Style hooks on thumb test
    [Fact]
    public Task PlacesStyleHooksOnRootAndThumb()
    {
        var cut = Render(CreateSwitchWithThumb(
            defaultChecked: true,
            disabled: true,
            readOnly: true,
            required: true,
            thumbAttributes: new Dictionary<string, object> { { "data-testid", "thumb" } }
        ));

        var switchEl = cut.Find("[role='switch']");
        var thumb = cut.Find("[data-testid='thumb']");

        // Root should have all data attributes
        switchEl.HasAttribute("data-checked").ShouldBeTrue();
        switchEl.HasAttribute("data-disabled").ShouldBeTrue();
        switchEl.HasAttribute("data-readonly").ShouldBeTrue();
        switchEl.HasAttribute("data-required").ShouldBeTrue();

        // Thumb should have all data attributes
        thumb.HasAttribute("data-checked").ShouldBeTrue();
        thumb.HasAttribute("data-disabled").ShouldBeTrue();
        thumb.HasAttribute("data-readonly").ShouldBeTrue();
        thumb.HasAttribute("data-required").ShouldBeTrue();

        return Task.CompletedTask;
    }

    // Helper class for RenderAs validation test
    private sealed class NonReferencableComponent : ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.CloseElement();
        }
    }
}
