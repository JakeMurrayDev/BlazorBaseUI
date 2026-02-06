namespace BlazorBaseUI.Tests.Button;

public class ButtonTests : BunitContext, IButtonContract
{
    public ButtonTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupButtonModule(JSInterop);
    }

    private RenderFragment CreateButton(
        bool disabled = false,
        bool focusableWhenDisabled = false,
        bool nativeButton = true,
        int tabIndex = 0,
        string? asElement = null,
        Type? renderAs = null,
        Func<ButtonState, string?>? classValue = null,
        Func<ButtonState, string?>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<BlazorBaseUI.Button.Button>(0);
            var attrIndex = 1;

            builder.AddAttribute(attrIndex++, "Disabled", disabled);
            builder.AddAttribute(attrIndex++, "FocusableWhenDisabled", focusableWhenDisabled);
            builder.AddAttribute(attrIndex++, "NativeButton", nativeButton);
            builder.AddAttribute(attrIndex++, "TabIndex", tabIndex);

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
            if (childContent is not null)
                builder.AddAttribute(attrIndex++, "ChildContent", childContent);

            builder.CloseComponent();
        };
    }

    // Rendering

    [Fact]
    public Task RendersAsButtonByDefault()
    {
        var cut = Render(CreateButton());
        var button = cut.Find("button");
        button.TagName.ShouldBe("BUTTON");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomAs()
    {
        var cut = Render(CreateButton(nativeButton: false, asElement: "span"));
        var element = cut.Find("span");
        element.TagName.ShouldBe("SPAN");
        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildContent()
    {
        var cut = Render(CreateButton(
            childContent: b => b.AddContent(0, "Click me")));
        var button = cut.Find("button");
        button.TextContent.ShouldBe("Click me");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateButton(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "my-button" },
                { "aria-label", "Submit form" }
            }));
        var button = cut.Find("button");
        button.GetAttribute("data-testid").ShouldBe("my-button");
        button.GetAttribute("aria-label").ShouldBe("Submit form");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateButton(
            classValue: _ => "custom-button"));
        var button = cut.Find("button");
        button.GetAttribute("class").ShouldContain("custom-button");
        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateButton(
            styleValue: _ => "color: red"));
        var button = cut.Find("button");
        button.GetAttribute("style").ShouldContain("color: red");
        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateButton(
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

    // Native button attributes

    [Fact]
    public Task NativeButton_HasTypeButton()
    {
        var cut = Render(CreateButton());
        var button = cut.Find("button");
        button.GetAttribute("type").ShouldBe("button");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_HasDisabledAttributeWhenDisabled()
    {
        var cut = Render(CreateButton(disabled: true));
        var button = cut.Find("button");
        button.HasAttribute("disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_DoesNotHaveAriaDisabledWhenDisabled()
    {
        var cut = Render(CreateButton(disabled: true));
        var button = cut.Find("button");
        button.HasAttribute("aria-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_DoesNotHaveRoleButton()
    {
        var cut = Render(CreateButton());
        var button = cut.Find("button");
        button.HasAttribute("role").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // Non-native button attributes

    [Fact]
    public Task NonNativeButton_HasRoleButton()
    {
        var cut = Render(CreateButton(nativeButton: false, asElement: "span"));
        var element = cut.Find("[role='button']");
        element.ShouldNotBeNull();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_DoesNotHaveTypeButton()
    {
        var cut = Render(CreateButton(nativeButton: false, asElement: "span"));
        var element = cut.Find("span");
        element.HasAttribute("type").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_HasAriaDisabledTrueWhenDisabled()
    {
        var cut = Render(CreateButton(nativeButton: false, asElement: "span", disabled: true));
        var element = cut.Find("span");
        element.GetAttribute("aria-disabled").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_HasTabIndexMinusOneWhenDisabled()
    {
        var cut = Render(CreateButton(nativeButton: false, asElement: "span", disabled: true));
        var element = cut.Find("span");
        element.GetAttribute("tabindex").ShouldBe("-1");
        return Task.CompletedTask;
    }

    // Data attributes

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateButton(disabled: true));
        var button = cut.Find("button");
        button.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotHaveDataDisabledWhenNotDisabled()
    {
        var cut = Render(CreateButton());
        var button = cut.Find("button");
        button.HasAttribute("data-disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    // FocusableWhenDisabled - native

    [Fact]
    public Task NativeFocusableWhenDisabled_DoesNotHaveDisabledAttribute()
    {
        var cut = Render(CreateButton(disabled: true, focusableWhenDisabled: true));
        var button = cut.Find("button");
        button.HasAttribute("disabled").ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeFocusableWhenDisabled_HasAriaDisabledTrue()
    {
        var cut = Render(CreateButton(disabled: true, focusableWhenDisabled: true));
        var button = cut.Find("button");
        button.GetAttribute("aria-disabled").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeFocusableWhenDisabled_HasTabIndex()
    {
        var cut = Render(CreateButton(disabled: true, focusableWhenDisabled: true, tabIndex: 3));
        var button = cut.Find("button");
        button.GetAttribute("tabindex").ShouldBe("3");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeFocusableWhenDisabled_HasDataDisabled()
    {
        var cut = Render(CreateButton(disabled: true, focusableWhenDisabled: true));
        var button = cut.Find("button");
        button.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // FocusableWhenDisabled - non-native

    [Fact]
    public Task NonNativeFocusableWhenDisabled_HasAriaDisabledTrue()
    {
        var cut = Render(CreateButton(nativeButton: false, asElement: "span", disabled: true, focusableWhenDisabled: true));
        var element = cut.Find("span");
        element.GetAttribute("aria-disabled").ShouldBe("true");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeFocusableWhenDisabled_HasTabIndex()
    {
        var cut = Render(CreateButton(nativeButton: false, asElement: "span", disabled: true, focusableWhenDisabled: true, tabIndex: 2));
        var element = cut.Find("span");
        element.GetAttribute("tabindex").ShouldBe("2");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeFocusableWhenDisabled_HasRoleButton()
    {
        var cut = Render(CreateButton(nativeButton: false, asElement: "span", disabled: true, focusableWhenDisabled: true));
        var element = cut.Find("span");
        element.GetAttribute("role").ShouldBe("button");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeFocusableWhenDisabled_HasDataDisabled()
    {
        var cut = Render(CreateButton(nativeButton: false, asElement: "span", disabled: true, focusableWhenDisabled: true));
        var element = cut.Find("span");
        element.HasAttribute("data-disabled").ShouldBeTrue();
        return Task.CompletedTask;
    }

    // TabIndex

    [Fact]
    public Task NativeButton_HasDefaultTabIndex()
    {
        var cut = Render(CreateButton());
        var button = cut.Find("button");
        button.GetAttribute("tabindex").ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeButton_HasDefaultTabIndex()
    {
        var cut = Render(CreateButton(nativeButton: false, asElement: "span"));
        var element = cut.Find("span");
        element.GetAttribute("tabindex").ShouldBe("0");
        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsExplicitTabIndex()
    {
        var cut = Render(CreateButton(tabIndex: 5));
        var button = cut.Find("button");
        button.GetAttribute("tabindex").ShouldBe("5");
        return Task.CompletedTask;
    }

    [Fact]
    public Task NonNativeDisabled_HasTabIndexMinusOne()
    {
        var cut = Render(CreateButton(nativeButton: false, asElement: "span", disabled: true));
        var element = cut.Find("span");
        element.GetAttribute("tabindex").ShouldBe("-1");
        return Task.CompletedTask;
    }

    // State cascading

    [Fact]
    public Task CascadesButtonStateToClassValue()
    {
        ButtonState? capturedState = null;
        var cut = Render(CreateButton(
            classValue: state =>
            {
                capturedState = state;
                return "test-class";
            }));
        capturedState.ShouldNotBeNull();
        capturedState!.Disabled.ShouldBeFalse();
        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesButtonStateDisabledTrue()
    {
        ButtonState? capturedState = null;
        var cut = Render(CreateButton(
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

    // Element reference

    [Fact]
    public Task ExposesElementReference()
    {
        BlazorBaseUI.Button.Button? component = null;
        var cut = Render(builder =>
        {
            builder.OpenComponent<BlazorBaseUI.Button.Button>(0);
            builder.AddComponentReferenceCapture(1, obj => component = (BlazorBaseUI.Button.Button)obj);
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
                builder.OpenComponent<BlazorBaseUI.Button.Button>(0);
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
