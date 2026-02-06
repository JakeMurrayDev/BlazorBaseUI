namespace BlazorBaseUI.Tests.Toggle;

public class ToggleTests : BunitContext, IToggleContract
{
    public ToggleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupToggleModule(JSInterop);
    }

    private RenderFragment CreateToggle(
        bool? pressed = null,
        bool defaultPressed = false,
        bool disabled = false,
        bool nativeButton = true,
        string? asElement = null,
        Type? renderAs = null,
        Func<ToggleState, string>? classValue = null,
        Func<ToggleState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Action<TogglePressedChangeEventArgs>? onPressedChange = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<BlazorBaseUI.Toggle.Toggle>(0);
            var attrIndex = 1;

            if (pressed.HasValue)
                builder.AddAttribute(attrIndex++, "Pressed", pressed.Value);
            if (defaultPressed)
                builder.AddAttribute(attrIndex++, "DefaultPressed", true);
            builder.AddAttribute(attrIndex++, "Disabled", disabled);
            builder.AddAttribute(attrIndex++, "NativeButton", nativeButton);

            if (asElement is not null)
                builder.AddAttribute(attrIndex++, "As", asElement);
            if (renderAs is not null)
                builder.AddAttribute(attrIndex++, "RenderAs", renderAs);
            if (classValue is not null)
                builder.AddAttribute(attrIndex++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(attrIndex++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
            if (onPressedChange is not null)
                builder.AddAttribute(attrIndex++, "OnPressedChange", EventCallback.Factory.Create(this, onPressedChange));
            if (childContent is not null)
                builder.AddAttribute(attrIndex++, "ChildContent", childContent);

            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsButtonByDefault()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");
        button.TagName.ShouldBe("BUTTON");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomAs()
    {
        var cut = Render(CreateToggle(nativeButton: false, asElement: "span"));
        var element = cut.Find("span");
        element.TagName.ShouldBe("SPAN");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateToggle(
            childContent: b => b.AddContent(0, "Toggle me")));
        var button = cut.Find("button");
        button.TextContent.ShouldBe("Toggle me");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateToggle(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "my-toggle" },
                { "aria-label", "Toggle bold" }
            }));
        var button = cut.Find("button");
        button.GetAttribute("data-testid").ShouldBe("my-toggle");
        button.GetAttribute("aria-label").ShouldBe("Toggle bold");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateToggle(
            classValue: _ => "custom-toggle"));
        var button = cut.Find("button");
        button.GetAttribute("class").ShouldContain("custom-toggle");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateToggle(
            styleValue: _ => "color: red"));
        var button = cut.Find("button");
        button.GetAttribute("style").ShouldContain("color: red");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateToggle(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }));
        var button = cut.Find("button");
        var classAttr = button.GetAttribute("class");
        classAttr.ShouldContain("static-class");
        classAttr.ShouldContain("dynamic-class");
        return Task.CompletedTask;
    }

    // ARIA

    [Fact]
    public Task HasAriaPressedFalseByDefault()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");
        button.GetAttribute("aria-pressed").ShouldBe("false");
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaPressedTrueWhenPressed()
    {
        var cut = Render(CreateToggle(pressed: true));
        var button = cut.Find("button");
        button.GetAttribute("aria-pressed").ShouldBe("true");
        return Task.CompletedTask;
    }

    // Native button attributes

    [Fact]
    public Task NativeButton_HasTypeButton()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");
        button.GetAttribute("type").ShouldBe("button");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_HasDisabledWhenDisabled()
    {
        var cut = Render(CreateToggle(disabled: true));
        var button = cut.Find("button");
        button.HasAttribute("disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_DoesNotHaveAriaDisabled()
    {
        var cut = Render(CreateToggle(disabled: true));
        var button = cut.Find("button");
        button.HasAttribute("aria-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_DoesNotHaveRoleButton()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");
        button.HasAttribute("role").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // Non-native button attributes

    [Fact]
    public Task NonNativeButton_HasRoleButton()
    {
        var cut = Render(CreateToggle(nativeButton: false, asElement: "span"));
        var element = cut.Find("[role='button']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_DoesNotHaveType()
    {
        var cut = Render(CreateToggle(nativeButton: false, asElement: "span"));
        var element = cut.Find("span");
        element.HasAttribute("type").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_HasAriaDisabledWhenDisabled()
    {
        var cut = Render(CreateToggle(nativeButton: false, asElement: "span", disabled: true));
        var element = cut.Find("span");
        element.GetAttribute("aria-disabled").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_HasTabIndexMinusOneWhenDisabled()
    {
        var cut = Render(CreateToggle(nativeButton: false, asElement: "span", disabled: true));
        var element = cut.Find("span");
        element.GetAttribute("tabindex").ShouldBe("-1");
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataPressedWhenPressed()
    {
        var cut = Render(CreateToggle(pressed: true));
        var button = cut.Find("button");
        button.HasAttribute("data-pressed").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataPressedWhenNotPressed()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");
        button.HasAttribute("data-pressed").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateToggle(disabled: true));
        var button = cut.Find("button");
        button.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataDisabledWhenNotDisabled()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");
        button.HasAttribute("data-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // TabIndex

    [Fact]
    public Task NativeButton_HasDefaultTabIndex()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");
        button.GetAttribute("tabindex").ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_HasDefaultTabIndex()
    {
        var cut = Render(CreateToggle(nativeButton: false, asElement: "span"));
        var element = cut.Find("span");
        element.GetAttribute("tabindex").ShouldBe("0");
        return Task.CompletedTask;
    }

    // State cascading

    [Fact]
    public Task ClassValueReceivesToggleState()
    {
        ToggleState? capturedState = null;
        var cut = Render(CreateToggle(
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }));
        capturedState.ShouldNotBeNull();
        capturedState!.Pressed.ShouldBeFalse();
        capturedState!.Disabled.ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ClassValueReceivesPressedTrue()
    {
        ToggleState? capturedState = null;
        var cut = Render(CreateToggle(
            pressed: true,
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }));
        capturedState.ShouldNotBeNull();
        capturedState!.Pressed.ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task ClassValueReceivesDisabledTrue()
    {
        ToggleState? capturedState = null;
        var cut = Render(CreateToggle(
            disabled: true,
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }));
        capturedState.ShouldNotBeNull();
        capturedState!.Disabled.ShouldBeTrue();
        return Task.CompletedTask;
    }

    // Uncontrolled state

    [Fact]
    public Task Uncontrolled_DefaultPressedTrue()
    {
        var cut = Render(CreateToggle(defaultPressed: true));
        var button = cut.Find("button");
        button.GetAttribute("aria-pressed").ShouldBe("true");
        button.HasAttribute("data-pressed").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task Uncontrolled_TogglesOnClick()
    {
        var cut = Render(CreateToggle());
        var button = cut.Find("button");

        button.GetAttribute("aria-pressed").ShouldBe("false");

        button.Click();

        button.GetAttribute("aria-pressed").ShouldBe("true");
        button.HasAttribute("data-pressed").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // Controlled state

    [Fact]
    public Task Controlled_ReflectsParentState()
    {
        // Pressed = true
        var cutTrue = Render(CreateToggle(pressed: true));
        var buttonTrue = cutTrue.Find("button");
        buttonTrue.GetAttribute("aria-pressed").ShouldBe("true");

        // Pressed = false
        var cutFalse = Render(CreateToggle(pressed: false));
        var buttonFalse = cutFalse.Find("button");
        buttonFalse.GetAttribute("aria-pressed").ShouldBe("false");

        return Task.CompletedTask;
    }

    // OnPressedChange

    [Fact]
    public Task OnPressedChange_FiresOnClick()
    {
        bool? receivedValue = null;
        var cut = Render(CreateToggle(
            onPressedChange: args => receivedValue = args.Pressed));

        var button = cut.Find("button");
        button.Click();

        receivedValue.ShouldNotBeNull();
        receivedValue.ShouldBe(true);
        return Task.CompletedTask;
    }

    [Fact]
    public Task Disabled_OnPressedChangeDoesNotFire()
    {
        var callCount = 0;
        var cut = Render(CreateToggle(
            disabled: true,
            onPressedChange: _ => callCount++));

        var button = cut.Find("button");
        button.Click();

        callCount.ShouldBe(0);
        return Task.CompletedTask;
    }

    // Element reference

    [Fact]
    public Task ExposesElementReference()
    {
        BlazorBaseUI.Toggle.Toggle? component = null;
        var cut = Render(builder =>
        {
            builder.OpenComponent<BlazorBaseUI.Toggle.Toggle>(0);
            builder.AddComponentReferenceCapture(1, obj => component = (BlazorBaseUI.Toggle.Toggle)obj);
            builder.CloseComponent();
        });
        component.ShouldNotBeNull();
        cut.WaitForState(() => component!.Element.HasValue);
        component!.Element.HasValue.ShouldBeTrue();
        return Task.CompletedTask;
    }

    // RenderAs validation

    [Fact]
    public Task ThrowsWhenRenderAsDoesNotImplementInterface()
    {
        Should.Throw<InvalidOperationException>(() =>
        {
            Render(builder =>
            {
                builder.OpenComponent<BlazorBaseUI.Toggle.Toggle>(0);
                builder.AddAttribute(1, "RenderAs", typeof(NonReferencableComponent));
                builder.CloseComponent();
            });
        });
        return Task.CompletedTask;
    }

    private sealed class NonReferencableComponent : ComponentBase
    {
        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.CloseElement();
        }
    }
}
