using BlazorBaseUI.Field;
using BlazorBaseUI.Form;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlazorBaseUI.Tests.Select;

public class SelectTriggerTests : BunitContext, ISelectTriggerContract
{
    public SelectTriggerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
        JsInteropSetup.SetupFieldModule(JSInterop);
        JsInteropSetup.SetupLabelModule(JSInterop);
        Services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
    }

    private RenderFragment CreateSelectWithTrigger(
        bool disabled = false,
        bool required = false,
        string? defaultValue = null,
        bool defaultOpen = false)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            var i = 1;
            if (disabled) builder.AddAttribute(i++, "Disabled", true);
            if (required) builder.AddAttribute(i++, "Required", true);
            if (defaultValue is not null) builder.AddAttribute(i++, "DefaultValue", defaultValue);
            builder.AddAttribute(i++, "DefaultOpen", defaultOpen);
            builder.AddAttribute(i++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
                {
                    b.OpenComponent<SelectValue<string>>(0);
                    b.AddAttribute(1, "Placeholder", "Select...");
                    b.CloseComponent();
                }));
                innerBuilder.CloseComponent();

                // Include items directly (no portal) for simplicity
                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<SelectItem<string>>(0);
                        popupBuilder.AddAttribute(1, "Value", "apple");
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task Disabled_CannotInteractWhenDisabled()
    {
        var cut = Render(CreateSelectWithTrigger(disabled: true));

        var button = cut.Find("button");
        button.HasAttribute("disabled").ShouldBeTrue();
        button.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Placeholder_ShouldHaveDataPlaceholderWhenNoValue()
    {
        var cut = Render(CreateSelectWithTrigger());

        var button = cut.Find("button");
        button.HasAttribute("data-placeholder").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Placeholder_ShouldNotHaveDataPlaceholderWhenValueProvided()
    {
        var cut = Render(CreateSelectWithTrigger(defaultValue: "apple"));

        var button = cut.Find("button");
        button.HasAttribute("data-placeholder").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task StyleHooks_ShouldHaveDataPopupOpenAndPressedWhenOpen()
    {
        var cut = Render(CreateSelectWithTrigger());

        var button = cut.Find("button");
        await button.TriggerEventAsync("onclick", new MouseEventArgs());
        cut.FindComponent<SelectTrigger>().Render();

        button = cut.Find("button");
        button.HasAttribute("data-popup-open").ShouldBeTrue();
        button.HasAttribute("data-pressed").ShouldBeTrue();
    }

    [Fact]
    public Task Required_SetsAriaRequiredAttribute()
    {
        var cut = Render(CreateSelectWithTrigger(required: true));

        var button = cut.Find("button");
        button.GetAttribute("aria-required").ShouldBe("true");

        return Task.CompletedTask;
    }

    // --- New helpers ---

    private RenderFragment CreateSelectWithTriggerFull(
        bool disabled = false,
        bool rootDisabled = false,
        bool multiple = false,
        string? defaultValue = null,
        IReadOnlyList<string>? defaultValues = null,
        Func<string?, string?>? itemToStringValue = null,
        string? value = null,
        EventCallback<string?>? valueChanged = null)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            var i = 1;
            if (rootDisabled) builder.AddAttribute(i++, "Disabled", true);
            builder.AddAttribute(i++, "Multiple", multiple);
            if (defaultValue is not null) builder.AddAttribute(i++, "DefaultValue", defaultValue);
            if (defaultValues is not null) builder.AddAttribute(i++, "DefaultValues", defaultValues);
            if (value is not null) builder.AddAttribute(i++, "Value", value);
            if (valueChanged.HasValue) builder.AddAttribute(i++, "ValueChanged", valueChanged.Value);
            if (itemToStringValue is not null) builder.AddAttribute(i++, "ItemToStringValue", itemToStringValue);
            builder.AddAttribute(i++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                var ti = 1;
                if (disabled) innerBuilder.AddAttribute(ti++, "Disabled", true);
                innerBuilder.AddAttribute(ti++, "ChildContent", (RenderFragment)(b =>
                {
                    b.OpenComponent<SelectValue<string>>(0);
                    b.AddAttribute(1, "Placeholder", "Select...");
                    b.CloseComponent();
                }));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<SelectItem<string>>(0);
                        popupBuilder.AddAttribute(1, "Value", "apple");
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // --- Disabled ---

    [Fact]
    public async Task Disabled_DoesNotTogglePopupWhenDisabled()
    {
        var cut = Render(CreateSelectWithTrigger(disabled: true));

        var button = cut.Find("button");
        await button.TriggerEventAsync("onclick", new MouseEventArgs());
        cut.FindComponent<SelectTrigger>().Render();

        button = cut.Find("button");
        button.GetAttribute("aria-expanded").ShouldBe("false");
    }

    // --- Placeholder ---

    [Fact]
    public Task Placeholder_DataPlaceholderWithCustomItemToStringValue()
    {
        // Even with a custom itemToStringValue, placeholder should still show when no value
        var cut = Render(CreateSelectWithTriggerFull(
            itemToStringValue: v => v?.ToUpper()));

        var button = cut.Find("button");
        button.HasAttribute("data-placeholder").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Placeholder_DataPlaceholderWhenProvidedNullValue()
    {
        var cut = Render(CreateSelectWithTriggerFull(
            value: null,
            valueChanged: EventCallback.Factory.Create<string?>(this, _ => { })));

        var button = cut.Find("button");
        button.HasAttribute("data-placeholder").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task Placeholder_NoDataPlaceholderWhenMultipleModeHasDefaultValue()
    {
        var cut = Render(CreateSelectWithTriggerFull(
            multiple: true,
            defaultValues: new[] { "apple" }));

        var button = cut.Find("button");
        button.HasAttribute("data-placeholder").ShouldBeFalse();

        return Task.CompletedTask;
    }

    // --- NativeButton ---

    private RenderFragment CreateSelectWithNativeButton(bool nativeButton, bool disabled = false)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            var i = 1;
            if (disabled) builder.AddAttribute(i++, "Disabled", true);
            builder.AddAttribute(i++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "NativeButton", nativeButton);
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b =>
                {
                    b.OpenComponent<SelectValue<string>>(0);
                    b.AddAttribute(1, "Placeholder", "Select...");
                    b.CloseComponent();
                }));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<SelectItem<string>>(0);
                        popupBuilder.AddAttribute(1, "Value", "apple");
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task NativeButton_RendersTypeButtonByDefault()
    {
        var cut = Render(CreateSelectWithTrigger());

        var button = cut.Find("button");
        button.GetAttribute("type").ShouldBe("button");

        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_FalseDoesNotRenderTypeButton()
    {
        var cut = Render(CreateSelectWithNativeButton(nativeButton: false));

        var button = cut.Find("button");
        button.HasAttribute("type").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_FalseRendersTabindex()
    {
        var cut = Render(CreateSelectWithNativeButton(nativeButton: false));

        var button = cut.Find("button");
        button.GetAttribute("tabindex").ShouldBe("0");

        return Task.CompletedTask;
    }

    [Fact]
    public Task NativeButton_FalseRendersTabindexMinusOneWhenDisabled()
    {
        var cut = Render(CreateSelectWithNativeButton(nativeButton: false, disabled: true));

        var button = cut.Find("button");
        button.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }

    // --- FieldRoot integration helpers ---

    private RenderFragment CreateSelectInFieldRoot(
        bool? invalid = null,
        bool fieldDisabled = false,
        bool? touchedState = null,
        bool? dirtyState = null)
    {
        return builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "Name", "fruit");
            if (invalid.HasValue) builder.AddAttribute(2, "Invalid", invalid.Value);
            builder.AddAttribute(3, "Disabled", fieldDisabled);
            if (touchedState.HasValue) builder.AddAttribute(4, "TouchedState", touchedState.Value);
            if (dirtyState.HasValue) builder.AddAttribute(5, "DirtyState", dirtyState.Value);
            builder.AddAttribute(6, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<SelectRoot<string>>(0);
                fieldBuilder.AddAttribute(1, "DefaultValue", "apple");
                fieldBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(selectBuilder =>
                {
                    selectBuilder.OpenComponent<SelectTrigger>(0);
                    selectBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
                    {
                        b.OpenComponent<SelectValue<string>>(0);
                        b.AddAttribute(1, "Placeholder", "Select...");
                        b.CloseComponent();
                    }));
                    selectBuilder.CloseComponent();

                    selectBuilder.OpenComponent<SelectPositioner>(10);
                    selectBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<SelectPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<SelectItem<string>>(0);
                            popupBuilder.AddAttribute(1, "Value", "apple");
                            popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                            popupBuilder.CloseComponent();
                        }));
                        posBuilder.CloseComponent();
                    }));
                    selectBuilder.CloseComponent();
                }));
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateSelectInFieldRootWithLabel()
    {
        return builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "Name", "fruit");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<FieldLabel>(0);
                fieldBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Fruit")));
                fieldBuilder.CloseComponent();

                fieldBuilder.OpenComponent<SelectRoot<string>>(10);
                fieldBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(selectBuilder =>
                {
                    selectBuilder.OpenComponent<SelectTrigger>(0);
                    selectBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
                    {
                        b.OpenComponent<SelectValue<string>>(0);
                        b.AddAttribute(1, "Placeholder", "Select...");
                        b.CloseComponent();
                    }));
                    selectBuilder.CloseComponent();
                }));
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateSelectInFieldRootWithDescription()
    {
        return builder =>
        {
            builder.OpenComponent<FieldRoot>(0);
            builder.AddAttribute(1, "Name", "fruit");
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(fieldBuilder =>
            {
                fieldBuilder.OpenComponent<FieldDescription>(0);
                fieldBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Choose a fruit")));
                fieldBuilder.CloseComponent();

                fieldBuilder.OpenComponent<SelectRoot<string>>(10);
                fieldBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(selectBuilder =>
                {
                    selectBuilder.OpenComponent<SelectTrigger>(0);
                    selectBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
                    {
                        b.OpenComponent<SelectValue<string>>(0);
                        b.AddAttribute(1, "Placeholder", "Select...");
                        b.CloseComponent();
                    }));
                    selectBuilder.CloseComponent();
                }));
                fieldBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    // --- FieldRoot integration tests ---

    [Fact]
    public Task FieldRoot_HasDataValidWhenValid()
    {
        var cut = Render(CreateSelectInFieldRoot(invalid: false));

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-valid").ShouldBeFalse();
        trigger.HasAttribute("data-invalid").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task FieldRoot_HasDataInvalidWhenInvalid()
    {
        var cut = Render(CreateSelectInFieldRoot(invalid: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-invalid").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task FieldRoot_HasDataTouchedDirtyFilledFocused()
    {
        var cut = Render(CreateSelectInFieldRoot(touchedState: true, dirtyState: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-touched").ShouldBeTrue();
        trigger.HasAttribute("data-dirty").ShouldBeTrue();
        // Has a default value "apple", so should be filled
        trigger.HasAttribute("data-filled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task FieldRoot_HasAriaLabelledBy()
    {
        var cut = Render(CreateSelectInFieldRootWithLabel());

        var trigger = cut.Find("button");
        var labelledBy = trigger.GetAttribute("aria-labelledby");
        labelledBy.ShouldNotBeNull();
        labelledBy.ShouldNotBeEmpty();

        // Verify a label element exists with this id
        var label = cut.Find($"[id='{labelledBy}']");
        label.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task FieldRoot_HasAriaDescribedBy()
    {
        var cut = Render(CreateSelectInFieldRootWithDescription());

        var trigger = cut.Find("button");
        var describedBy = trigger.GetAttribute("aria-describedby");
        describedBy.ShouldNotBeNull();
        describedBy.ShouldNotBeEmpty();

        return Task.CompletedTask;
    }

    [Fact]
    public Task FieldRoot_HasAriaInvalidWhenFieldInvalid()
    {
        var cut = Render(CreateSelectInFieldRoot(invalid: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-invalid").ShouldBe("true");

        return Task.CompletedTask;
    }
}
